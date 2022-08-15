using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace SimpleFacialTracker
{
    /// <summary>
    /// 簡易Facial Tracker
    /// </summary>
    internal class SimpleFacialTracker : IDisposable
    {
        public static readonly Guid ServiceUuid = CreateFullUuid(0x1069);
        public static readonly Guid CharacteristicUuid = CreateFullUuid(0x7777);
        public static readonly Guid DescriptorUuid = CreateFullUuid(0x2902);

        private BluetoothLEDevice? _device;
        private GattDeviceService? _gattService;
        private GattCharacteristic? _gattCharacteristic;

        /// <summary>
        /// Bluetoothのアドレス
        /// </summary>
        public ulong BluetoothAddress { get; }

        /// <summary>
        /// 接続済みか
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// トラッカーの値が変化した時に発火するイベント
        /// </summary>
        public event Action<TrackingData>? ValueChanged;

        /// <summary>
        /// Bluetoothのアドレスを指定してインスタンスを作成します．
        /// </summary>
        /// <param name="bluetoothAddress"></param>
        public SimpleFacialTracker(ulong bluetoothAddress)
        {
            BluetoothAddress = bluetoothAddress;
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// トラッカーと接続します．
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect()
        {
            if (IsConnected) { return true; }

            // 指定したアドレスのデバイスと接続
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(BluetoothAddress);

            // Trackerサービスを取得
            var servicesResult = await _device.GetGattServicesForUuidAsync(ServiceUuid);
            if (!servicesResult.Services.Any())
            {
                Disconnect();
                return false;
            }
            _gattService = servicesResult.Services[0];

            // トラッキング値のCharacteristicを取得して，変更通知を購読
            var result = await _gattService.GetCharacteristicsForUuidAsync(CharacteristicUuid);
            if (!result.Characteristics.Any())
            {
                Disconnect();
                return false;
            }

            _gattCharacteristic = result.Characteristics[0];
            _gattCharacteristic.ValueChanged += CharacteristicValueChanged;

            var status = await _gattCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success)
            {
                Disconnect();
                return false;
            }

            IsConnected = true;
            return true;
        }

        /// <summary>
        /// トラッカーを切断します．
        /// </summary>
        public void Disconnect()
        {
            if (_gattCharacteristic is not null)
            {
                _gattCharacteristic.ValueChanged -= CharacteristicValueChanged;
            }

            if (_gattService is not null)
            {
                _gattService.Dispose();
                _gattService = null;
            }

            if (_device is not null)
            {
                _device.Dispose();
                _device = null;
            }

            IsConnected = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TrackingData> ReadAsync()
        {
            if (!IsConnected || _gattCharacteristic is null) { throw new InvalidOperationException(); }

            var result = await _gattCharacteristic.ReadValueAsync();

            return new TrackingData(DateTime.Now, ReadDataFromBuffer(result.Value));
        }

        /// <summary>
        /// トラッカーの値変化時の処理を行います．
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = ReadDataFromBuffer(args.CharacteristicValue);
            ValueChanged?.Invoke(new TrackingData(args.Timestamp.LocalDateTime, data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static byte[] ReadDataFromBuffer(IBuffer buffer)
        {
            var reader = DataReader.FromBuffer(buffer);
            var data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);

            return data;
        }

        /// <summary>
        /// 短縮されたUUIDからフルサイズのUUIDを作成します．
        /// </summary>
        /// <param name="shortUuid">短縮されたUUID</param>
        /// <returns>UUID</returns>
        private static Guid CreateFullUuid(uint shortUuid)
        {
            return new Guid(shortUuid, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5f, 0x9b, 0x34, 0xfb);
        }
    }

    /// <summary>
    /// トラッキング情報
    /// </summary>
    /// <param name="Timestamp">データの取得時刻</param>
    /// <param name="RawData">生データのバイト配列</param>
    internal record TrackingData(DateTime Timestamp, byte[] RawData)
    {
    }
}
