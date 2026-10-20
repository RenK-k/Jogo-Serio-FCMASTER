using UnityEngine;

namespace CoachMaster.UI
{
    [CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Coach Master/Audio Library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Header("Interface")]
        public AudioClip Click;

        [Header("Jogo")]
        public AudioClip Goal;
        public AudioClip Whistle;
        public AudioClip Foul;
        public AudioClip Injury;
        public AudioClip YellowCard;
        public AudioClip RedCard;
        public AudioClip Corner;
        public AudioClip ThrowIn;
        public AudioClip Kickoff;
        public AudioClip Crowd;

        [Header("Reações da bancada")]
        public AudioClip HomeGoal;
        public AudioClip AwayGoal;
        public AudioClip BicycleKick;
        public AudioClip ShotWide;
        public AudioClip ShotPost;
        public AudioClip BigSave;
        public AudioClip Penalty;

        [Header("Música (até 10 faixas)")]
        public AudioClip[] MusicTracks = new AudioClip[10];
    }
}
