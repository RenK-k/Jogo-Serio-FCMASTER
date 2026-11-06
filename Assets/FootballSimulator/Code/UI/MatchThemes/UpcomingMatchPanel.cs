using System;
using UnityEngine;
using FStudio.UI.Events;
using FStudio.Events;
using FStudio.UI.MatchThemes.MatchEvents;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Enums;
using FStudio.MatchEngine.Tactics;
using TMPro;
using FStudio.UI.Panels;

namespace FStudio.UI.MatchThemes {
    public class UpcomingMatchPanel : EventPanel<UpcomingMatchEvent> {
        [SerializeField] private TeamVisual[] teams = new TeamVisual[2];
        [SerializeField] private TextMeshProUGUI difficultyText;

        private UpcomingMatchEvent eventObject;

        /// <summary>
        /// home kit or away kit.
        /// </summary>
        private bool[] kits = new bool[2];

        protected override void OnEnable() {
            base.OnEnable();
            if (canvasGroup != null && !IsActive)
                canvasGroup.alpha = 0f;
        }

        protected override async void OnEventCalled(UpcomingMatchEvent eventObject) {
            this.eventObject = eventObject;

            if (eventObject == null) {
                Disappear();
                return;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            Debug.Log("Upcoming match");

            EventManager.Trigger(new LoadingEvent("PREPARING_MATCH"));

            await teams [0].SetTeam(
                eventObject.details.homeTeam,
                eventObject.details.homeTeam.Formation,
                eventObject.details.homeTeam.Players);

            await teams [1].SetTeam(
                eventObject.details.awayTeam,
                eventObject.details.awayTeam.Formation,
                eventObject.details.awayTeam.Players);

            kits[0] = false; // set home teams kit to main kit.
            kits[1] = true; // set away teams kit to side kit.

            UpdateKits();
            teams[0].SetCrest(CareerTacticLock.HomeCrest);
            teams[1].SetCrest(CareerTacticLock.AwayCrest);
            if (CareerTacticLock.Portuguese)
                Translate(transform);
            if (CareerTacticLock.CareerChoice && MatchSettingsPanel.SIDE != Shared.Responses.MatchCreateRequest.UserTeam.None
                && MatchSettingsPanel.SIDE != CareerTacticLock.OwnSide)
                MatchSettingsPanel.SIDE = Shared.Responses.MatchCreateRequest.UserTeam.None;

            Appear();

            EventManager.Trigger<LoadingEvent>(null);
        }

        private void UpdateKits () {
            var homeKit = !kits[0] ? eventObject.details.homeTeam.HomeKit : eventObject.details.homeTeam.AwayKit;
            var awayKit = !kits[1] ? eventObject.details.awayTeam.HomeKit : eventObject.details.awayTeam.AwayKit;

            for (int i = 0; i < 2; i++) {
                var targetKit = i == 0 ? homeKit : awayKit;
                teams[i].KitSolver.SetKit(
                    targetKit.PreviewTexture,
                    targetKit.Color1,
                    targetKit.Color2);
            }
        }

        public async void StartMatch () {
            Disappear();

            // update the details.
            var details = eventObject.details;
            details.aiLevel = MatchSettingsPanel.AILEVEL;
            if (!CareerTacticLock.CareerChoice)
                details.dayTime = MatchSettingsPanel.DAYTIMES;
            details.userTeam = MatchSettingsPanel.SIDE;
            eventObject.details = details;
            //

            try {
                await MatchEngineLoader.Current.StartMatchEngine(
                    eventObject,
                    kits[0],
                    kits[1]);
                if (details.userTeam == Shared.Responses.MatchCreateRequest.UserTeam.None)
                    CareerTacticLock.WatchSimulation?.Invoke();
            }
            catch (Exception error) {
                Debug.LogException(error);
                CareerTacticLock.ReturnToMatchDay?.Invoke();
            }
        }

        public void SwitchKit (int index) {
            kits[index] = !kits[index];

            UpdateKits();
        }

        static void Translate(Transform root) {
            var labels = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (var i = 0; i < labels.Length; i++) {
                var text = labels[i].text;
                if (text == "Line up")
                    labels[i].text = "Escalação";
                else if (text == "START MATCH")
                    labels[i].text = "COMEÇAR PARTIDA";
            }
        }
    }
}

