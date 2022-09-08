using System.Collections.Generic;

public class CommandLineArgs
{
    public class Args
    {
        Dictionary<string, float> m_args;

        internal Args() { m_args = new Dictionary<string, float>(); }

        public float GetWithDefault(string key, float defaultVal)
        {
            if (m_args.ContainsKey(key)) return m_args[key];
            else return defaultVal;
        }

        public void Add(string key, float val)
        {
            m_args[key] = val;
        }
    }

    private static CommandLineArgs instance = null;

    Args m_args;

    private CommandLineArgs() { m_args = new Args(); }

    public static CommandLineArgs Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CommandLineArgs();
            }
            return instance;
        }
    }

    public Args Parameters { get { return m_args; }
    }
}
