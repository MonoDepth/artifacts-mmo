using api_mmo.ArtifactsApi;

namespace api_mmo.PlayerControllers;

public class SmartController(SmartCharacter smartCharacter, PlayerCharacter character) : PlayerBaseController(character)
{
    private SmartCharacter _smartCharacter = smartCharacter;
    public void Reload(SmartCharacter smartCharacter)
    {
        StopCycle();
        _smartCharacter = smartCharacter;
        _actions.Clear();
        _ = StartCycle();
        LogMsg("Reloaded character");
    }

    protected override void DecideNextAction()
    {
        EvaluationEngine eval = new(this);
        if (LastActionResult.IsFailure)
        {
            if (_smartCharacter.OnFailure.TryGetValue(LastActionResult.GetResultName(), out var failureAction))
            {
                LogMsg($"Last action failed, performing failure action {LastActionResult.GetResultName()}");
                foreach (var actionDo in failureAction.Do)
                {
                    var actionFunc = eval.DoAction(actionDo);
                    if (actionFunc == null)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        _actions.Enqueue(actionFunc);
                    }
                }
            }
            else
            {
                LogMsg($"Last action failed with {LastActionResult.GetMessage()}, waiting 5 seconds before retrying");
                Thread.Sleep(5000);
            }
        }

        foreach (var action in _smartCharacter.Actions)
        {
            if (action.While != "" && eval.EvaluateCondition(action.While))
            {
                LogMsg($"Performing action {action.Name}");
                while (eval.EvaluateCondition(action.While))
                {
                    foreach (var actionDo in action.Do)
                    {
                        var actionFunc = eval.DoAction(actionDo);
                        if (actionFunc == null)
                        {
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            _actions.Enqueue(actionFunc);
                        }
                    }
                }
                if (!action.Cascade)
                    return;
            }
            else if (action.If != "" && eval.EvaluateCondition(action.If))
            {
                LogMsg($"Executing {action.Name}");
                foreach (var actionDo in action.Do)
                {
                        var actionFunc = eval.DoAction(actionDo);
                        if (actionFunc == null)
                        {
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            _actions.Enqueue(actionFunc);
                        }
                }

                if (!action.Cascade)
                    return;
            }
            else if (action.If == "")
            {
                LogMsg($"Executing {action.Name}");
                foreach (var actionDo in action.Do)
                {
                    var actionFunc = eval.DoAction(actionDo);
                    if (actionFunc == null)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        _actions.Enqueue(actionFunc);
                    }
                }

                if (!action.Cascade)
                    return;
            }
        }
    }
}