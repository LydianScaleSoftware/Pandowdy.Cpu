using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Pandowdy.Core
{
    // Basic stub representing a single soft switch

    public class CountableVariable<T>(T initialValue)
    {
        protected T _value = initialValue;
        protected int _count = 0;

        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    _count++;
                }
            }
        }
        public int Count => _count;
        public void ResetCount() => _count = 0;
        public override string ToString()
        {
            if (Value != null)
            {
                return Value.ToString() + $" ({_count})";
            }
            else
            {
                return $"null ({_count})";
            }
        }
        public static string ToDebugString(CountableVariable<T> variable)
        {
            return $"Value: {variable._value}, ChangeCount: {variable._count}";
        }
    }

    public class CountableBool : CountableVariable<bool>
    {
        public CountableBool() : base(false) { }

        public void Set() { Value = true; }
        public void Clear() { Value = false; }
        public void Toggle() { Value = !Value; }
    }


    public sealed class SoftSwitch(string name) : CountableBool
    {

        public string Name { get; private set; } = name;


        public override string ToString() => $"{Name}: {base.Value}";
    }




    // Basic stub collection for soft switches
    public sealed class SoftSwitchCollection
    {
        public enum SoftSwitchId
        {
            Store80,
            RamRd,
            RamWrt,
            AltZp,
            HiRes,
            Page2,
            IntCxRom,
            SlotC3Rom,
            HighWrite,
            Bank1,
            HighRead
        }

        private Dictionary<SoftSwitchId, SoftSwitch> _switches = [];
        public SoftSwitchCollection()
        {
            _switches[SoftSwitchId.Store80] = new SoftSwitch("80STORE");
            _switches[SoftSwitchId.RamRd] = new SoftSwitch("RAMRD");
            _switches[SoftSwitchId.RamWrt] = new SoftSwitch("RAMWRT");
            _switches[SoftSwitchId.AltZp] = new SoftSwitch("ALTZP");
            _switches[SoftSwitchId.HiRes] = new SoftSwitch("HIRES");
            _switches[SoftSwitchId.Page2] = new SoftSwitch("PAGE2");
            _switches[SoftSwitchId.IntCxRom] = new SoftSwitch("INTCXROM");
            _switches[SoftSwitchId.SlotC3Rom] = new SoftSwitch("SLOTC3ROM");
            _switches[SoftSwitchId.HighWrite] = new SoftSwitch("HIGHWRITE");
            _switches[SoftSwitchId.Bank1] = new SoftSwitch("BANK1");
            _switches[SoftSwitchId.HighRead] = new SoftSwitch("HIGHREAD");
        }

        public void Set(SoftSwitchId id, bool value)
        {
            if (_switches.TryGetValue(id, out var softSwitch))
            {
                softSwitch.Value = value;
            }
        }

        public List<(SoftSwitchId id, bool value, int count)> GetSwitchList()
        {
            var result = new List<(SoftSwitchId id, bool value, int count)>();
            foreach (var kvp in _switches)
            {
                result.Add((kvp.Key, kvp.Value.Value, kvp.Value.Count));
            }
            return result;
        }

        public void ResetSwitchUsageCounts()
        {
            foreach (var kvp in _switches)
            {
                kvp.Value.ResetCount();
            }
        }
    }
}
