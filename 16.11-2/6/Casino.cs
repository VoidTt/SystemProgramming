using System.Text;
namespace Homework;

public static class Casino
{
    public static string Run(int total, CancellationToken token)
    {
        if (total < 20 || total > 100) throw new ArgumentOutOfRangeException(nameof(total));
        using var seats = new Semaphore(5, 5);
        using var endOfDay = new ManualResetEventSlim(false);
        var players = new Thread[total];
        var balances = new int[total];
        int played = 0;
        int seated = 0, maximumSeated = 0;
        object table = new();
        Exception? error = null;
        for (int i = 0; i < total; i++)
        {
            int index = i;
            players[i] = new Thread(() => {
                bool hasSeat = false;
                int balance = 100;
                try
                {
                    // Ожидание стула также можно отменить при закрытии окна.
                    while (!seats.WaitOne(50)) token.ThrowIfCancellationRequested();
                    hasSeat = true;
                    lock (table) { seated++; maximumSeated = Math.Max(maximumSeated, seated); }
                    bool firstRound = true;
                    while (balance > 0 && !endOfDay.IsSet)
                    {
                        token.ThrowIfCancellationRequested();
                        int bet = Random.Shared.Next(1, balance + 1);
                        int chosenNumber = Random.Shared.Next(0, 37);
                        lock (table)
                        {
                            // Выплата x2 ставки: ставка возвращается с такой же прибылью.
                            int ball = Random.Shared.Next(0, 37);
                            balance += ball == chosenNumber ? bet : -bet;
                            if (firstRound)
                            {
                                firstRound = false;
                                played++;
                                if (played == total) endOfDay.Set();
                            }
                        }
                        if (token.WaitHandle.WaitOne(10)) token.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { lock (table) { error ??= ex; } }
                finally
                {
                    balances[index] = balance;
                    if (hasSeat) { lock (table) seated--; seats.Release(); }
                }
            }) { IsBackground = true };
            players[i].Start();
        }
        foreach (var player in players) player.Join();
        token.ThrowIfCancellationRequested();
        if (error != null) throw error;
        var report = new StringBuilder();
        report.AppendLine($"Игроков: {total}; сыграли: {played}; максимум за столом: {maximumSeated}");
        report.AppendLine("Игрок | Начальная сумма | Конечная сумма");
        for (int i = 0; i < total; i++) report.AppendLine($"Игрок{i+1}   100   {balances[i]}");
        return report.ToString();
    }
}

