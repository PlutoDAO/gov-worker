using System;

namespace PlutoDAO.Gov.Worker.Entities
{
    public class Option : IEquatable<Option>
    {
        public readonly string Name;

        public Option(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentNullException(nameof(name)) : name;
        }

        public bool Equals(Option other)
        {
            if (other is null) return false;
            return other.Name == Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Option);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }
}
