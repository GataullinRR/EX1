using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;
using Common;
using DeviceBase.Devices;

namespace DeviceBase.Models
{
    public class RequestInfo
    {
        readonly internal Func<RequestInfo, IEnumerable<IDataEntity>> _entitiesFactory;

        internal RUSDeviceId DeviceId { get; }
        internal Command Address { get; }
        internal EntityDescriptor[] Descriptors { get; }
        /// <summary>
        /// This entities are meant to be entered manually by user
        /// </summary>
        public EntityDescriptor[] UserCommandDescriptors { get; }

        internal RequestInfo(RUSDeviceId deviceId, Command address, EntityDescriptor[] descriptors)
            : this(deviceId, address, descriptors, descriptors.Length.Range(), _ => new IDataEntity[0])
        {

        }
        internal RequestInfo(RUSDeviceId deviceId, Command address, EntityDescriptor[] descriptors, IEnumerable<int> userCommandDescriptors, Func<RequestInfo, IEnumerable<IDataEntity>> entitiesFactory)
        {
            _entitiesFactory = entitiesFactory ?? (_ => new IDataEntity[0]);
            DeviceId = deviceId;
            Address = address;
            Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            UserCommandDescriptors = Descriptors.SubArray(userCommandDescriptors).ToArray();
        }
        
        public IEnumerable<IDataEntity> BuildWriteRequestBody(IEnumerable<IDataEntity> setEntities)
        {
            var allEntities = setEntities.Concat(_entitiesFactory(this)).MakeCached();
            foreach (var descriptor in Descriptors)
            {
                yield return allEntities.Find(e => e.Descriptor.Name == descriptor.Name).ValueOrDefault ?? descriptor.DefaultEntity;
            }
        }
    }
}
