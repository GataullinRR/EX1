using Calibrators;

namespace RUSTelemetryStreamSenderExports
{
    public interface IDecoratedDataProvider : IDataProvider
    {
        event DecoratedDataRowAquiredDelegate DecoratedDataRowAquired;
    }
}