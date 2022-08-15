using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace SimpleFacialTracker
{
    internal class SimpleFacialTrackerScanner
    {
        /// <summary>
        /// Advertisement監視オブジェクト
        /// </summary>
        private readonly BluetoothLEAdvertisementWatcher _watcher = new();

        /// <summary>
        /// スキャンタスク
        /// </summary>
        private TaskCompletionSource? _scanTcs;

        /// <summary>
        /// Advertisementを受信したデバイスのアドレスリスト
        /// </summary>
        private readonly HashSet<ulong> _foundDeviceAddresses = new();

        /// <summary>
        /// トラッカーのリスト
        /// </summary>
        private readonly List<SimpleFacialTracker> _trackers = new();

        /// <summary>
        /// 既定のタイムアウト時間でスキャンを開始します．
        /// </summary>
        /// <returns>発見したトラッカーのリスト</returns>
        public async Task<IReadOnlyList<SimpleFacialTracker>> Scan()
        {
            return await Scan(TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// タイムアウト時間を指定してスキャンを開始します．
        /// </summary>
        /// <param name="timeout">タイムアウト時間</param>
        /// <returns>発見したトラッカーのリスト</returns>
        public async Task<IReadOnlyList<SimpleFacialTracker>> Scan(TimeSpan timeout)
        {
            _watcher.Received += OnWatcherReceived;
            _watcher.ScanningMode = BluetoothLEScanningMode.Active;
            _scanTcs = new TaskCompletionSource();

            Console.WriteLine("Scanning...");
            Console.WriteLine($"Simple Face Tracker Service UUID is {SimpleFacialTracker.ServiceUuid}");

            _watcher.Start();
            await Task.WhenAny(_scanTcs.Task, Task.Delay(timeout));
            _watcher.Stop();

            return _trackers.AsReadOnly();
        }

        /// <summary>
        /// Advertisement受信時の処理を行います．
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // 既にAdvertisement受信済みの機器であれば何もしない
            if (!_foundDeviceAddresses.Add(args.BluetoothAddress))
            {
                return;
            }

            Console.WriteLine($"[{args.Advertisement.LocalName} ({args.BluetoothAddress})]");

            var bleServiceUuids = args.Advertisement.ServiceUuids;
            foreach (var bleServiceUuid in bleServiceUuids)
            {
                Console.WriteLine($"Service UUID: {bleServiceUuid}");
                // トラッカーのサービスUUIDと一致すれば，トラッカーリストに追加
                if (bleServiceUuid == SimpleFacialTracker.ServiceUuid)
                {
                    Console.WriteLine("Tracker Found!");
                    _trackers.Add(new SimpleFacialTracker(args.BluetoothAddress));
                    _scanTcs?.SetResult();
                }
            }
        }

    }
}
