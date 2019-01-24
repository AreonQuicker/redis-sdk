using System;
using System.Collections.Generic;

namespace Cache.Objects
{
    public class ConnectionValue : IEquatable<ConnectionValue>
    {
        public string Field { get; set; }
        public string Value { get; set; }
        public int SortIndex { get; set; }

        public ConnectionValueType ConnectionValueType { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionValue);
        }

        public bool Equals(ConnectionValue other)
        {
            return other != null &&
                   Field == other.Field &&
                   Value == other.Value &&
                   ConnectionValueType == other.ConnectionValueType &&
                   SortIndex == other.SortIndex;
        }

        public override int GetHashCode()
        {
            var hashCode = 996675367;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Field);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + ConnectionValueType.GetHashCode();
            hashCode = hashCode * -1521134295 + SortIndex.GetHashCode();

            return hashCode;
        }

        public static bool operator ==(ConnectionValue value1, ConnectionValue value2)
        {
            return EqualityComparer<ConnectionValue>.Default.Equals(value1, value2);
        }

        public static bool operator !=(ConnectionValue value1, ConnectionValue value2)
        {
            return !(value1 == value2);
        }
    }
}
