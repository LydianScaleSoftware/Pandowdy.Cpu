using Pandowdy.EmuCore.DataTypes;
using Pandowdy.EmuCore.Interfaces;
using Pandowdy.EmuCore.Services;
using System.Reactive.Linq;

namespace Pandowdy.EmuCore.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the telemetry system - cross-thread scenarios, multiple publishers/subscribers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> These tests verify the telemetry system works correctly in
/// realistic multi-threaded scenarios where devices publish from background threads and
/// ViewModels subscribe and receive messages.
/// </para>
/// <para>
/// <strong>Test Categories:</strong>
/// <list type="bullet">
/// <item>Cross-thread publishing from multiple concurrent publishers</item>
/// <item>Multiple subscribers receiving all messages</item>
/// <item>High-volume message delivery without loss</item>
/// <item>Subscription disposal behavior</item>
/// <item>Interface segregation verification</item>
/// </list>
/// </para>
/// </remarks>
public class TelemetryIntegrationTests
{
    #region Cross-Thread Integration Tests

    [Fact]
    public async Task CrossThread_MultiplePublishers_AllMessagesReceived()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var received = new System.Collections.Concurrent.ConcurrentBag<TelemetryMessage>();
        
        aggregator.Stream.Subscribe(m => received.Add(m));

        // Act - 4 concurrent publishers, each publishing 25 messages
        var tasks = new List<Task>();
        for (int publisher = 0; publisher < 4; publisher++)
        {
            var publisherId = aggregator.CreateId($"Publisher{publisher}");
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 25; i++)
                {
                    aggregator.Publish(new TelemetryMessage(publisherId, "event", i));
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Assert - All 100 messages should be received
        Assert.Equal(100, received.Count);
    }

    [Fact]
    public async Task CrossThread_MultipleSubscribers_AllReceiveMessages()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var subscriber1 = new System.Collections.Concurrent.ConcurrentBag<TelemetryMessage>();
        var subscriber2 = new System.Collections.Concurrent.ConcurrentBag<TelemetryMessage>();
        var subscriber3 = new System.Collections.Concurrent.ConcurrentBag<TelemetryMessage>();
        
        aggregator.Stream.Subscribe(m => subscriber1.Add(m));
        aggregator.Stream.Subscribe(m => subscriber2.Add(m));
        aggregator.Stream.Subscribe(m => subscriber3.Add(m));

        var deviceId = aggregator.CreateId("TestDevice");

        // Act - Publish from multiple threads
        var tasks = new List<Task>();
        for (int t = 0; t < 3; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    aggregator.Publish(new TelemetryMessage(deviceId, "event", i));
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Assert - All subscribers should receive all 30 messages
        Assert.Equal(30, subscriber1.Count);
        Assert.Equal(30, subscriber2.Count);
        Assert.Equal(30, subscriber3.Count);
    }

    [Fact]
    public async Task CrossThread_PublishAndSubscribe_ConcurrentOperations()
    {
        // Arrange - Subscribe, publish, and create IDs all concurrently
        var aggregator = new TelemetryAggregator();
        var received = new System.Collections.Concurrent.ConcurrentBag<TelemetryMessage>();
        var ids = new System.Collections.Concurrent.ConcurrentBag<TelemetryId>();
        var subscriptionCount = 0;
        
        // Start with initial subscriber
        aggregator.Stream.Subscribe(m => received.Add(m));

        // Act - Concurrent operations
        var tasks = new List<Task>();
        
        // Task 1: Create IDs
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                ids.Add(aggregator.CreateId($"Device{i}"));
            }
        }));
        
        // Task 2: Publish messages
        tasks.Add(Task.Run(async () =>
        {
            var id = aggregator.CreateId("Publisher");
            for (int i = 0; i < 50; i++)
            {
                aggregator.Publish(new TelemetryMessage(id, "event", i));
                if (i % 10 == 0)
                {
                    await Task.Yield();
                }
            }
        }));
        
        // Task 3: Add more subscribers
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 5; i++)
            {
                aggregator.Stream.Subscribe(m => Interlocked.Increment(ref subscriptionCount));
            }
        }));
        
        await Task.WhenAll(tasks);

        // Assert - All IDs created and at least some messages received
        Assert.Equal(20, ids.Count);
        Assert.True(ids.Select(id => id.Id).Distinct().Count() == 20, "All IDs should be unique");
        Assert.True(received.Count > 0, "Should have received messages");
    }

    [Fact]
    public async Task CrossThread_HighVolume_NoMessageLoss()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var received = new System.Collections.Concurrent.ConcurrentBag<int>();
        var deviceId = aggregator.CreateId("HighVolumeDevice");
        
        aggregator.Stream
            .Where(m => m.SourceId.Id == deviceId.Id)
            .Subscribe(m => received.Add((int)m.Payload!));

        // Act - High volume publishing from background thread
        const int messageCount = 1000;
        await Task.Run(() =>
        {
            for (int i = 0; i < messageCount; i++)
            {
                aggregator.Publish(new TelemetryMessage(deviceId, "data", i));
            }
        });

        // Assert - All messages should be received
        Assert.Equal(messageCount, received.Count);
        
        // Verify all sequence numbers are present
        var sorted = received.OrderBy(x => x).ToList();
        for (int i = 0; i < messageCount; i++)
        {
            Assert.Equal(i, sorted[i]);
        }
    }

    #endregion

    #region ITelemetryStream Interface Tests

    [Fact]
    public void ITelemetryStream_ImplementedByTelemetryAggregator()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();

        // Act & Assert - TelemetryAggregator should implement ITelemetryStream
        ITelemetryStream stream = aggregator;
        Assert.NotNull(stream);
        Assert.NotNull(stream.Stream);
    }

    [Fact]
    public void ITelemetryStream_CanSubscribeViaBaseInterface()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        ITelemetryStream readOnlyStream = aggregator;
        var received = new List<TelemetryMessage>();
        
        readOnlyStream.Stream.Subscribe(m => received.Add(m));
        
        // Act - Publish via full interface
        var id = aggregator.CreateId("TestDevice");
        aggregator.Publish(new TelemetryMessage(id, "test", 42));

        // Assert - Should receive via read-only interface
        Assert.Single(received);
        Assert.Equal("test", received[0].MessageType);
    }

    [Fact]
    public void ITelemetryStream_DoesNotExposeCreateIdOrPublish()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        ITelemetryStream readOnlyStream = aggregator;

        // Act & Assert - ITelemetryStream should NOT have CreateId or Publish
        var streamType = typeof(ITelemetryStream);
        Assert.Null(streamType.GetMethod("CreateId"));
        Assert.Null(streamType.GetMethod("Publish"));
        
        // Verify it only has Stream property
        var properties = streamType.GetProperties();
        Assert.Single(properties);
        Assert.Equal("Stream", properties[0].Name);
    }

    [Fact]
    public void ITelemetryAggregator_InheritsFromITelemetryStream()
    {
        // Assert - Interface inheritance
        Assert.True(typeof(ITelemetryStream).IsAssignableFrom(typeof(ITelemetryAggregator)));
    }

    #endregion

    #region Subscription Disposal Tests

    [Fact]
    public void Subscribe_Dispose_StopsReceivingMessages()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var received = new List<TelemetryMessage>();
        var deviceId = aggregator.CreateId("TestDevice");
        
        var subscription = aggregator.Stream.Subscribe(m => received.Add(m));
        
        // Publish first message
        aggregator.Publish(new TelemetryMessage(deviceId, "before", 1));
        
        // Act - Dispose subscription
        subscription.Dispose();
        
        // Publish second message
        aggregator.Publish(new TelemetryMessage(deviceId, "after", 2));

        // Assert - Should only have first message
        Assert.Single(received);
        Assert.Equal("before", received[0].MessageType);
    }

    [Fact]
    public void Subscribe_DisposeOneOfMany_OthersStillReceive()
    {
        // Arrange
        var aggregator = new TelemetryAggregator();
        var received1 = new List<TelemetryMessage>();
        var received2 = new List<TelemetryMessage>();
        var deviceId = aggregator.CreateId("TestDevice");
        
        var sub1 = aggregator.Stream.Subscribe(m => received1.Add(m));
        var sub2 = aggregator.Stream.Subscribe(m => received2.Add(m));
        
        // Publish first message
        aggregator.Publish(new TelemetryMessage(deviceId, "first", 1));
        
        // Dispose first subscription
        sub1.Dispose();
        
        // Act - Publish second message
        aggregator.Publish(new TelemetryMessage(deviceId, "second", 2));

        // Assert
        Assert.Single(received1); // Only received before dispose
        Assert.Equal(2, received2.Count); // Received both
    }

    #endregion

    #region Multi-Device Integration Scenarios

    [Fact]
    public void Scenario_MultipleDeviceTypes_FilterByCategory()
    {
        // Arrange - Simulate a system with multiple device types
        var aggregator = new TelemetryAggregator();
        var diskMessages = new List<TelemetryMessage>();
        var printerMessages = new List<TelemetryMessage>();
        
        var disk1 = aggregator.CreateId("DiskII");
        var disk2 = aggregator.CreateId("DiskII");
        var printer = aggregator.CreateId("Printer");
        
        aggregator.Stream
            .Where(m => m.SourceId.Category == "DiskII")
            .Subscribe(m => diskMessages.Add(m));
        
        aggregator.Stream
            .Where(m => m.SourceId.Category == "Printer")
            .Subscribe(m => printerMessages.Add(m));

        // Act - Simulate device activity
        aggregator.Publish(new TelemetryMessage(disk1, "motor", true));
        aggregator.Publish(new TelemetryMessage(printer, "status", "ready"));
        aggregator.Publish(new TelemetryMessage(disk2, "motor", true));
        aggregator.Publish(new TelemetryMessage(disk1, "track", 5));
        aggregator.Publish(new TelemetryMessage(printer, "page", 1));
        aggregator.Publish(new TelemetryMessage(disk2, "track", 10));

        // Assert - Each subscriber only receives relevant messages
        Assert.Equal(4, diskMessages.Count);
        Assert.Equal(2, printerMessages.Count);
        Assert.All(diskMessages, m => Assert.Equal("DiskII", m.SourceId.Category));
        Assert.All(printerMessages, m => Assert.Equal("Printer", m.SourceId.Category));
    }

    [Fact]
    public void Scenario_FilterBySpecificDevice_AmongSameCategory()
    {
        // Arrange - Two disk drives, monitor only drive 1
        var aggregator = new TelemetryAggregator();
        var drive1Messages = new List<TelemetryMessage>();
        
        var drive1Id = aggregator.CreateId("DiskII");
        var drive2Id = aggregator.CreateId("DiskII");
        
        aggregator.Stream
            .Where(m => m.SourceId.Id == drive1Id.Id)
            .Subscribe(m => drive1Messages.Add(m));

        // Act
        aggregator.Publish(new TelemetryMessage(drive1Id, "motor", true));
        aggregator.Publish(new TelemetryMessage(drive2Id, "motor", true));
        aggregator.Publish(new TelemetryMessage(drive1Id, "track", 5));
        aggregator.Publish(new TelemetryMessage(drive2Id, "track", 10));

        // Assert - Only drive 1 messages
        Assert.Equal(2, drive1Messages.Count);
        Assert.All(drive1Messages, m => Assert.Equal(drive1Id.Id, m.SourceId.Id));
    }

    [Fact]
    public async Task Scenario_SimulatedEmulatorWithUI_CrossThreadCommunication()
    {
        // Arrange - Simulate emulator thread publishing, UI thread receiving
        var aggregator = new TelemetryAggregator();
        var uiReceivedMessages = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var driveId = aggregator.CreateId("DiskII");
        
        // UI "thread" subscribes
        aggregator.Stream
            .Where(m => m.SourceId.Category == "DiskII")
            .Subscribe(m => uiReceivedMessages.Enqueue($"{m.MessageType}:{m.Payload}"));

        // Act - "Emulator thread" publishes disk activity
        await Task.Run(() =>
        {
            // Simulate disk boot sequence
            aggregator.Publish(new TelemetryMessage(driveId, "disk-inserted", "DOS33.dsk"));
            aggregator.Publish(new TelemetryMessage(driveId, "motor", true));
            
            for (int track = 0; track < 5; track++)
            {
                aggregator.Publish(new TelemetryMessage(driveId, "track", track));
                Thread.Sleep(1); // Simulate work
            }
            
            aggregator.Publish(new TelemetryMessage(driveId, "motor", false));
        });

        // Assert
        Assert.Equal(8, uiReceivedMessages.Count); // insert + motor on + 5 tracks + motor off
        
        // Verify sequence
        var messages = uiReceivedMessages.ToList();
        Assert.Equal("disk-inserted:DOS33.dsk", messages[0]);
        Assert.Equal("motor:True", messages[1]);
        Assert.Equal("track:0", messages[2]);
        Assert.Equal("motor:False", messages[7]);
    }

    #endregion
}
