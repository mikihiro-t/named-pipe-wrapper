using System;

namespace ExampleCLI
{
    class MyMessage
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return string.Format("\"{0}\" (message ID = {1})", Text, Id);
        }
    }
}