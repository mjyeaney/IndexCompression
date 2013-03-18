using System.Text;

namespace IndexCompression
{
    public class TestContext
    {
        StringBuilder _buffer;

        public TestContext()
        {
            _buffer = new StringBuilder();
        }

        public bool HasOutput { get; private set; }

        public void WriteLine(string text)
        {
            HasOutput = true;
            _buffer.Append("\t");
            _buffer.AppendLine(text);
        }

        public string GetContextOutput()
        {
            return _buffer.ToString();
        }
    }
}
