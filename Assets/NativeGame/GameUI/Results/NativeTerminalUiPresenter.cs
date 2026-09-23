using Echo.NativeGame.GameFlow.Ending;
using Echo.NativeGame.GameFlow.Results;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Stage-to-view glue only. NativeRunResultFlow owns the frozen result and the
    // allowed transition to Results; navigation remains with GF01-01/INT.
    public sealed class NativeTerminalUiPresenter : MonoBehaviour
    {
        NativeRunResultFlow flow;
        NativeHudView hud;
        NativeEndingView ending;
        NativeResultsView results;
        NativeBirthResultAction birthAction;

        public void Bind(NativeRunResultFlow resultFlow, NativeHudView hudView,
            NativeEndingView endingView, NativeResultsView resultsView,
            NativeBirthResultAction birthResultAction)
        {
            Unbind();
            flow = resultFlow;
            hud = hudView;
            ending = endingView;
            results = resultsView;
            birthAction = birthResultAction;
            if (flow) flow.StageChanged += OnStageChanged;
            if (ending) ending.ViewResultsRequested += ViewResults;
            if (birthAction) birthAction.ViewResultsRequested += ViewResults;
            OnStageChanged(flow ? flow.Stage : RunResultStage.None,
                flow ? flow.Snapshot : null);
        }

        // INT routes the tablet's existing physical return key here only while
        // BirthContinuation is active. Other tablet modes use their normal back path.
        public bool TryHandleTabletBack()
        {
            return flow && flow.Stage == RunResultStage.BirthContinuation && flow.ViewResults();
        }

        void ViewResults()
        {
            if (flow) flow.ViewResults();
        }

        void OnStageChanged(RunResultStage stage, RunResultSnapshot snapshot)
        {
            if (stage != RunResultStage.None && hud) hud.Show(false);
            if (ending)
            {
                if (stage == RunResultStage.EndingPresentation) ending.Bind(snapshot);
                ending.Show(stage == RunResultStage.EndingPresentation);
            }
            if (birthAction) birthAction.Show(stage == RunResultStage.BirthContinuation);
            if (results)
            {
                if (stage == RunResultStage.Results) results.Bind(snapshot);
                results.Show(stage == RunResultStage.Results);
            }
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (flow) flow.StageChanged -= OnStageChanged;
            if (ending) ending.ViewResultsRequested -= ViewResults;
            if (birthAction) birthAction.ViewResultsRequested -= ViewResults;
            flow = null;
            hud = null;
            ending = null;
            results = null;
            birthAction = null;
        }
    }
}
