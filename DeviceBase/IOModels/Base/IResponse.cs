using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public enum RequestStatus
    {
        NOT_PERFORMED = 0,
        OK,
        WRONG_CHECKSUM,
        [Description("Коммандное слово в ответе не совпадает с коммандным словом в запросе")]
        WRONG_HEADER,
        [Description("Устройство либо не ответило совсем, либо не переслало недостаточное количество данных")]
        READ_TIMEOUT,
        [Description("Несовпадение ожидаемой длины области данных ответа с фактической")]
        WRONG_LENGTH,
        [Description("Соединение было неожиданно прервано, либо пристутствовала иная ошибка при передаче, вероятно связанная с некоректной работой драйвера или преобразорвателя интерфейса")]
        CONNECTION_INTERFACE_ERROR,
        [Description("Запрос был прерван по инициативе программы (не ошибка)")]
        CANCELLED,
        [Description("Данные в ответе не совпали с ожидаемыми")]
        NOT_EXPECTED_RESPONSE_BYTES,
        DESERIALIZATION_ERROR,
        UNKNOWN_ERROR
    }

    public interface IResponse
    {
        IRequest Request { get; }
        RequestStatus Status { get; }
        /// <summary>
        /// Can be null
        /// </summary>
        IResponseData Data { get; }
        IDictionary<Key, IResponseData> DataSections { get; }
    }
}
