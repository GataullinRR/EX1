using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;
using Utilities.Types;

namespace DataRequestWidget
{
    /// <summary>
    /// Uses caching
    /// </summary>
    class DataPacketDTOBuilder
    {
        public const char SAME_NAME_ESCAPING = '*';
        const string DTO_PROPERTY_SUPPORTED_SYMBOLS = "+-=_-?/\\| ";

        /// <summary>
        /// For fixing memory leaks!
        /// </summary>
        static readonly List<ClassBuilder> _classes = new List<ClassBuilder>();

        public object Instantiate(IEnumerable<(string Property, string Value)> properties)
        {
            var propertiesInfos = new List<(string Name, Type Type)>();
            var values = new List<string>();
            foreach (var item in properties)
            {
                var fixedName = item.Property
                    .Select(c => (char.IsLetterOrDigit(c) || DTO_PROPERTY_SUPPORTED_SYMBOLS.Contains(c)) ? c : '?')
                    .Aggregate();
                while (propertiesInfos.Any(i => i.Name == fixedName))
                {
                    fixedName = fixedName + SAME_NAME_ESCAPING;
                }
                propertiesInfos.Add((fixedName, typeof(string)));
                values.Add(item.Value);
            }
            if (!_classes.Any(c => c.HasSameProperties(propertiesInfos)))
            {
                var dto = new ClassBuilder();
                propertiesInfos.ForEach(p => dto.AddProperty(p.Name, p.Type));
                dto.FinishBuilding();
                _classes.Add(dto);
            }

            return _classes
                .Find(c => c.HasSameProperties(propertiesInfos))
                .Instantiate(values);
        }
    }
}
