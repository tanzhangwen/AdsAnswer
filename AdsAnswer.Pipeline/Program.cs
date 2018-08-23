using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdsAnswer.AnswerBuilder;

namespace AdsAnswer.Pipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            FeedBuilder fb = new FeedBuilder();
            fb.Build();
        }
    }
}
