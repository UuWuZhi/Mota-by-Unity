using System;

public interface IEventRunner
{
    void Run(EventNode rootNode, EventNodeContext ctx, Action onComplete);
}
