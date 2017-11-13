namespace ScriptRunner.Core
{
    public interface ICommandExecutor
    {
        void Display(string command);

        void Execute();
    }
}
