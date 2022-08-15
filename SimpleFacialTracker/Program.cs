namespace SimpleFacialTracker;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Simple Facial Tracker");

        var scanner = new SimpleFacialTrackerScanner();
        var trackers = await scanner.Scan();

        if (!trackers.Any()) { return; }
        var tracker = trackers[0];
        tracker.ValueChanged += data => Console.WriteLine($"Notify: {data.Timestamp:yyyy/MM/dd HH:mm:ss.fff} {data.RawData[0]}");
        Console.WriteLine("Connecting...");
        await tracker.Connect();
        Console.WriteLine("Successfully connected!");

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    private static string CreateHexString(ReadOnlySpan<byte> bytes)
    {
        var builder = new System.Text.StringBuilder("0x", (bytes.Length + 1) * 2);
        foreach (var b in bytes)
        {
            builder.AppendFormat("{0:x2}", b);
        }

        return builder.ToString();
    }
}

