using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Calibrators.Models
{
    class CalibrationFileGenerator
    {
        readonly IRUSDevice _device;
        readonly FileType _calibrationFileType;
        readonly string _fileVersion;

        public CalibrationFileGenerator(IRUSDevice device, FileType calibrationFileType, string fileVersion)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _calibrationFileType = calibrationFileType;
            _fileVersion = fileVersion ?? throw new ArgumentNullException(nameof(fileVersion));
        }

        public async Task<IEnumerable<IDataEntity>> GenerateAsync(IEnumerable<CalibrationFileEntity> coefficients)
        {
            var result = await _device.ReadAsync(_calibrationFileType.GetInfo().RequestAddress, DeviceOperationScope.DEFAULT, CancellationToken.None);
            if (result.Status != ReadStatus.OK)
            {
                throw new InvalidOperationException("Не удалось запросить файл калибровки для определения серийного номера и модификации прибора.");
            }
            var serialNumber = Files.GetFileEntity(result.Entities, FileEntityType.SERIAL_NUMBER);
            var modification = Files.GetFileEntity(result.Entities, FileEntityType.MODIFICATION);

            return generateFile();

            IEnumerable<IDataEntity> generateFile()
            {
                var fileInfo = new FileDescriptorsTarget(_calibrationFileType, _fileVersion, _device.Id);
                foreach (var descriptor in Files.Descriptors[fileInfo].Descriptors)
                {
                    var entity = descriptor.FileDefaultDataEntity;
                    if (descriptor.ValueFormat == DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY)
                    {
                        entity = descriptor.InstantiateEntity(descriptor.Serialize(coefficients));
                    }

                    if (entity.Descriptor.Name == serialNumber.Descriptor.Name)
                    {
                        yield return serialNumber;
                    }
                    else if (entity.Descriptor.Name == modification.Descriptor.Name)
                    {
                        yield return modification;
                    }
                    else
                    {
                        yield return entity;
                    }
                }
            }
        }
    }
}
