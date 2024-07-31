using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BFTT.Components
{

    public class SimpleFootstep : MonoBehaviour
    {
        public AudioClip[] footsteps;
        [SerializeField] private AudioSource footstepAudioSource;

        public AudioClip[] shimmyClips;
        [SerializeField] private AudioSource shimmyAudioSource;

        public void Footstep(AnimationEvent evt)
        {
            footstepAudioSource.clip = footsteps[Random.Range(0, footsteps.Length)];

            if (evt.animatorClipInfo.weight > 0.5f)
                footstepAudioSource.Play();
        }

        public void Shimmy(AnimationEvent evt)
        {
            shimmyAudioSource.clip = shimmyClips[Random.Range(0, shimmyClips.Length)];

            if (evt.animatorClipInfo.weight > 0.5f)
                shimmyAudioSource.Play();
        }
    }
}