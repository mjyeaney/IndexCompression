using System;

namespace IndexCompression
{
    /// <summary>
    /// Some basic testing support.
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
                throw new Exception("Statement not true.");
            }
        }

        /// <summary>
        /// Ensures a statement is false.
        /// </summary>
        public static void IsFalse(Func<bool> expr)
        {
            if (expr())
            {
                throw new Exception("Statement not false.");
            }
        }

        /// <summary>
        /// Ensures two arguments are equal.
        /// </summary>
        public static void AreEqual<T>(T first, T second) 
        {
            if (!first.Equals(second))
            {
                throw new Exception("Arguments not equal.");
            }
        }

        /// <summary>
        /// Ensures two arguemnts are false.
        /// </summary>
        public static void AreNotEqual<T>(T first, T second)
        {
            if (first.Equals(second))
            {
                throw new Exception("Arguments are equal.");
            }
        }
    }
}
