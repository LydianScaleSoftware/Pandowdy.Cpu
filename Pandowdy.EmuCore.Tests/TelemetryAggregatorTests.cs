using Pandowdy.EmuCore.DataTypes;
using Pandowdy.EmuCore.Interfaces;
using Pandowdy.EmuCore.Services;
using System.Reactive.Linq;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Tests for TelemetryAggregator - central hub for device telemetry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Test Coverage:</strong>
/// <list type="bullet">
/// <item>ID creation and uniqueness</item>
/// <item>Category assignment</item>
/// <item>Message publishing and subscription</item>
/// <item>Thread safety of ID generation</item>
/// <item>Multiple subscribers</item>
/// </list>
/// </para>
/// </remarks>
public class TelemetryAggregatorTests
{
    #region TelemetryId Tests

    [Fact]
    public void TelemetryId_ToString_FormatsCorrectly()
    {
        // Arrange
        var id = new TelemetryId(42, "DiskII");

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal("DiskII:42", result);
    }

    [Fact]
    public void TelemetryId_Equality_WorksCorrectly()
    {
        // Arrange
        var id1 = new TelemetryId(1, "DiskII");
        var id2 = new TelemetryId(1, "DiskII");
        var id3 = new TelemetryId(2, "DiskII");
        var id4 = new TelemetryId(1, "Printer");

        // Assert
        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3); // Different ID
        Assert.NotEqual(id1, id4); // Different category
    }

    #endregion

    #region TelemetryMessage Tests

    [Fact]
    public void TelemetryMessage_ToString_WithPayload_FormatsCorrectly()
    {
        // Arrange
        var id = new TelemetryId(1, "DiskII");
        var message = new TelemetryMessage(id, "motor", true);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Equal("[DiskII:1] motor: True", result);
    }

    [Fact]
    public void TelemetryMessage_ToString_WithoutPayload_FormatsCorrectly()
    {
        // Arrange
        var id = new TelemetryId(1, "DiskII");
        var message = new TelemetryMessage(id, "disk-ejected");

        // Act
        var result = message.ToString();

        // Assert
        Assert.Equal("[DiskII:1] disk-ejected", result);
    }

    [Fact]
    public void TelemetryMessage_Equality_WorksCorrectly()
    {
        // Arrange
        var id = new TelemetryId(1, "DiskII");
        var msg1 = new TelemetryMessage(id, "motor", true);
        var msg2 = new TelemetryMessage(id, "motor", true);
        var msg3 = new TelemetryMessage(id, "motor", false);

        // Assert
        Assert.Equal(msg1, msg2);
        Assert.NotEqual(msg1, msg3);
    }

    #endregion

    #region CreateId Tests

    [Fact]
    public void CreateId_ReturnsIdWithSpecifiedCategory()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();

        // Act
        var id = aggregator.CreateId("DiskII");

        // Assert
        Assert.Equal("DiskII", id.Category);
    }

    [Fact]
    public void CreateId_ReturnsUniqueIds()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();

        // Act
        var id1 = aggregator.CreateId("DiskII");
        var id2 = aggregator.CreateId("DiskII");
        var id3 = aggregator.CreateId("Printer");

        // Assert
        Assert.NotEqual(id1.Id, id2.Id);
        Assert.NotEqual(id2.Id, id3.Id);
        Assert.NotEqual(id1.Id, id3.Id);
    }

    [Fact]
    public void CreateId_IdsAreSequential()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();

        // Act
        var id1 = aggregator.CreateId("A");
        var id2 = aggregator.CreateId("B");
        var id3 = aggregator.CreateId("C");

        // Assert
        Assert.Equal(1, id1.Id);
        Assert.Equal(2, id2.Id);
        Assert.Equal(3, id3.Id);
    }

    [Fact]
    public void CreateId_NullCategory_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.CreateId(null!));
    }

    [Fact]
    public async Task CreateId_ThreadSafe_GeneratesUniqueIds()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var ids = new List<TelemetryId>();
        var lockObj = new object();

        // Act - Create 100 IDs from multiple threads
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var id = aggregator.CreateId("Test");
                lock (lockObj)
                {
                    ids.Add(id);
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Assert - All IDs should be unique
        var uniqueIds = ids.Select(id => id.Id).Distinct().ToList();
        Assert.Equal(100, uniqueIds.Count);
    }

    #endregion

    #region Publish and Subscribe Tests

    [Fact]
    public void Publish_DeliversMessageToSubscriber()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var id = aggregator.CreateId("DiskII");
        TelemetryMessage? received = null;

        aggregator.Stream.Subscribe(m => received = m);

        // Act
        var message = new TelemetryMessage(id, "motor", true);
        aggregator.Publish(message);

        // Assert
        Assert.NotNull(received);
        Assert.Equal(message, received);
    }

    [Fact]
    public void Publish_DeliversToMultipleSubscribers()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var id = aggregator.CreateId("DiskII");
        var received1 = new List<TelemetryMessage>();
        var received2 = new List<TelemetryMessage>();

        aggregator.Stream.Subscribe(m => received1.Add(m));
        aggregator.Stream.Subscribe(m => received2.Add(m));

        // Act
        var message = new TelemetryMessage(id, "motor", true);
        aggregator.Publish(message);

        // Assert
        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Equal(message, received1[0]);
        Assert.Equal(message, received2[0]);
    }

    [Fact]
    public void Publish_MultipleMessages_DeliveredInOrder()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var id = aggregator.CreateId("DiskII");
        var received = new List<TelemetryMessage>();

        aggregator.Stream.Subscribe(m => received.Add(m));

        // Act
        aggregator.Publish(new TelemetryMessage(id, "motor", true));
        aggregator.Publish(new TelemetryMessage(id, "track", 10));
        aggregator.Publish(new TelemetryMessage(id, "motor", false));

        // Assert
        Assert.Equal(3, received.Count);
        Assert.Equal("motor", received[0].MessageType);
        Assert.Equal("track", received[1].MessageType);
        Assert.Equal("motor", received[2].MessageType);
    }

    [Fact]
    public void Stream_CanFilterByCategory()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var diskId = aggregator.CreateId("DiskII");
        var printerId = aggregator.CreateId("Printer");
        var diskMessages = new List<TelemetryMessage>();

        aggregator.Stream
            .Where(m => m.SourceId.Category == "DiskII")
            .Subscribe(m => diskMessages.Add(m));

        // Act
        aggregator.Publish(new TelemetryMessage(diskId, "motor", true));
        aggregator.Publish(new TelemetryMessage(printerId, "status", "ready"));
        aggregator.Publish(new TelemetryMessage(diskId, "track", 5));

        // Assert
        Assert.Equal(2, diskMessages.Count);
        Assert.All(diskMessages, m => Assert.Equal("DiskII", m.SourceId.Category));
    }

    [Fact]
    public void Stream_CanFilterByMessageType()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var id = aggregator.CreateId("DiskII");
        var motorMessages = new List<TelemetryMessage>();

        aggregator.Stream
            .Where(m => m.MessageType == "motor")
            .Subscribe(m => motorMessages.Add(m));

        // Act
        aggregator.Publish(new TelemetryMessage(id, "motor", true));
        aggregator.Publish(new TelemetryMessage(id, "track", 10));
        aggregator.Publish(new TelemetryMessage(id, "motor", false));

        // Assert
        Assert.Equal(2, motorMessages.Count);
    }

    [Fact]
    public void Stream_LateSubscriber_DoesNotReceivePastMessages()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var id = aggregator.CreateId("DiskII");

        // Publish before subscribing
        aggregator.Publish(new TelemetryMessage(id, "early", null));

        var received = new List<TelemetryMessage>();
        aggregator.Stream.Subscribe(m => received.Add(m));

        // Act
        aggregator.Publish(new TelemetryMessage(id, "after", null));

        // Assert - Only received the message after subscription
        Assert.Single(received);
        Assert.Equal("after", received[0].MessageType);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void Scenario_MultipleDevices_MessagesCorrectlyAttributed()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var drive1Id = aggregator.CreateId("DiskII");
        var drive2Id = aggregator.CreateId("DiskII");
        var received = new List<TelemetryMessage>();

        aggregator.Stream.Subscribe(m => received.Add(m));

        // Act - Simulate two drives sending telemetry
        aggregator.Publish(new TelemetryMessage(drive1Id, "motor", true));
        aggregator.Publish(new TelemetryMessage(drive2Id, "motor", true));
        aggregator.Publish(new TelemetryMessage(drive1Id, "track", 5));
        aggregator.Publish(new TelemetryMessage(drive2Id, "track", 10));

        // Assert
        Assert.Equal(4, received.Count);
        
        // Filter to drive 1 only
        var drive1Messages = received.Where(m => m.SourceId.Id == drive1Id.Id).ToList();
        Assert.Equal(2, drive1Messages.Count);
        Assert.Equal(5, drive1Messages.First(m => m.MessageType == "track").Payload);
        
        // Filter to drive 2 only
        var drive2Messages = received.Where(m => m.SourceId.Id == drive2Id.Id).ToList();
        Assert.Equal(2, drive2Messages.Count);
        Assert.Equal(10, drive2Messages.First(m => m.MessageType == "track").Payload);
    }

    [Fact]
    public void Scenario_TypicalDeviceUsage()
    {
        // Arrange - Simulate a DiskII drive's lifecycle
        var aggregator = new TelemetryAggregator();
        var driveId = aggregator.CreateId("DiskII");
        var events = new List<string>();

        aggregator.Stream
            .Where(m => m.SourceId.Id == driveId.Id)
            .Subscribe(m => events.Add($"{m.MessageType}:{m.Payload}"));

        // Act - Simulate disk insertion and reading
        aggregator.Publish(new TelemetryMessage(driveId, "disk-inserted", "DOS33.dsk"));
        aggregator.Publish(new TelemetryMessage(driveId, "motor", true));
        aggregator.Publish(new TelemetryMessage(driveId, "track", 0));
        aggregator.Publish(new TelemetryMessage(driveId, "track", 1));
        aggregator.Publish(new TelemetryMessage(driveId, "track", 2));
        aggregator.Publish(new TelemetryMessage(driveId, "motor", false));

        // Assert
        Assert.Equal(6, events.Count);
                        Assert.Equal("disk-inserted:DOS33.dsk", events[0]);
                        Assert.Equal("motor:True", events[1]);
                        Assert.Equal("track:0", events[2]);
                                Assert.Equal("motor:False", events[5]);
                            }

                            #endregion
                        }
