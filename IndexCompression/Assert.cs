using System;

namespace IndexCompression
{
    /// <summary>
    /// Some basic testing support - replace with your favorite 
    /// testing FX.
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// Ensures a statement is true.
        /// </summary>
        public static void IsTrue(Func<bool> expr)
        {
            if (!expr())
            {
                throw new Exception("Expression evaluated as false.");
            }
        }

        /// <summary>
        /// Ensures a statement is false.
        /// </summary>
        public static void IsFalse(Func<bool> expr)
        {
            if (expr())
            {
                throw new Exception("Expression evaluated as true.");
            }
        }

        /// <summary>
        /// Ensures two arguments are equal.
        /// </summary>
        public static void AreEqual<T>(T first, T second) 
        {
            if (!first.Equals(second))
            {
                throw new Exception(String.Format("Arguments not equal; {0} <> {1}", first, second));
            }
        }

        /// <summary>
        /// Ensures two arguemnts are false.
        /// </summary>
        public static void AreNotEqual<T>(T first, T second)
        {
            if (first.Equals(second))
            {
                throw new Exception(String.Format("Arguments are equal; {0} == {1}", first, second));
            }
        }
    }
}
