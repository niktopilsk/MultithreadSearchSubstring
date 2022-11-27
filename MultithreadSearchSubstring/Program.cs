using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultithreadSearchSubstring
{
    internal static class Program
    {
        private static readonly List<Task> Tasks = new();
        private const int ThreadsCount = 12;
        private static readonly ConcurrentDictionary<int, string> Result = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Path for file: ");
            var path = Console.ReadLine();
            Console.WriteLine("Substring for search: ");
            var substring = Console.ReadLine();
            RunSearch(path, substring);
            Task.WaitAll(Tasks.ToArray());
            var resultFilePath = Path.Combine(Path.Combine(Path.GetDirectoryName(path)!),
                $"Result of search \'{substring}\' in {Path.GetFileNameWithoutExtension(path)}.txt");
            await File.WriteAllTextAsync(resultFilePath, "");
            foreach (var (key, value) in Result)
            {
                await File.AppendAllTextAsync(resultFilePath, $"{key}: {value}\n");
            }
        }

        private static async void RunSearch(string path, string substring)
        {
            var index = 1;
            var lines = await File.ReadAllLinesAsync(path);
            var linesCount = lines.Length / ThreadsCount;
            for (var i = 0; i < ThreadsCount; i++)
            {
                var part = lines.Skip(linesCount * i + 1).Take(linesCount);
                var index1 = index;
                var task = Task.Run(async () =>
                {
                    await Seach(part, substring, index1);
                });
                index = linesCount * i + 1;
                Tasks.Add(task);
            }
        }

        private static Task Seach(
            IEnumerable<string> lines,
            string substr,
            int index)
        {
            try
            {
                foreach (var line in lines)
                {
                    if (line.Contains(substr, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Result.TryAdd(index, line);
                        Console.WriteLine($"{index} {line}");
                    }

                    index++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}