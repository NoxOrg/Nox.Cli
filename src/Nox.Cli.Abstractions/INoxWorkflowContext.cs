﻿namespace Nox.Cli.Abstractions
{
    public interface INoxWorkflowContext
    {
        Guid WorkflowId { get; init; }
        void AddToVariables(string key, object value);
        void SetErrorMessage(string errorMessage);
        void SetState(ActionState state);
    }
}