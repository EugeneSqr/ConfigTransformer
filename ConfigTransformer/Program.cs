namespace ConfigTransformer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                return;
            }

            var transformer = new SolutionConfigsTransformer(args[0], args[1], args[2]);
            if (args.Length > 3)
            {
                for (int i = 3; i < args.Length; i++)
                {
                    transformer.FilesToExclude.Add(args[i]);
                }
            }

            transformer.Transform();
        }
    }
}
