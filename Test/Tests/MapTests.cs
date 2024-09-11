﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ConcurrentUtils.Test
{
    [TestFixture]
    public class MapTests : BaseUnitTest
    {
        [Test]
        public async Task Should_Map_Async()
        {
            Console.WriteLine(" ⇒ Started");
            var rnd = new Random();
            var sourceStrings = new string[]
            {
                "Hello", "Hello world", "How are you", "I'm doing fine"
            };
            var source = new string[100];
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = sourceStrings[i%sourceStrings.Length];
            }
            Func<string, Task<int>> method = async (text) =>
            {
                await Task.Delay((int) Math.Floor(rnd.NextDouble()*100));
                return text.Length;
            };
            var mappedValues = await ConcurrentUtils.Map(source, 7, method);
            Assert.That(source.Length, Is.EqualTo(mappedValues.Length));
            for (var i = 0; i < source.Length; i++)
            {
                Assert.That(source[i].Length, Is.EqualTo(mappedValues[i]));
            }
            Console.WriteLine(" ⇒ Finished");
        }
    }
}
