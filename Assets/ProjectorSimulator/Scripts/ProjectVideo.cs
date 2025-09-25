using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace ProjectorSimulator
{
    public class ProjectVideo : MonoBehaviour
    {
        VideoClip _clip;
        AudioSource _audioSrc;
        bool _loop = true;
        bool _playOnAwake = true;

        VideoPlayer player;

        ProjectorSim pj;

        public void Init(VideoClip clip, AudioSource audioSource, RenderTexture rt, bool loop = true, bool playOnAwake = true)
        {
            _clip = clip;
            _audioSrc = audioSource;
            _loop = loop;
            _playOnAwake = playOnAwake;

            pj = GetComponent<ProjectorSim>();

            if (!_playOnAwake)
            {
                pj.enabled = false;
            }

            if (_clip)
            {
                // Set up the VideoPlayer - DON'T play on awake as we need to Prepare() our settings first.
                player = gameObject.AddComponent<VideoPlayer>();
                player.playOnAwake = false;
                if (_playOnAwake)
                    player.prepareCompleted += delegate { PlayAfterPrepared(); };
                else
                    pj.enabled = false;

                // Tell the VideoPlayer to render to the projector's RenderTexture
                player.renderMode = VideoRenderMode.RenderTexture;
                player.targetTexture = rt;

                // Tell the VideoPlayer to play our clip
                player.clip = _clip;
                player.isLooping = _loop;

                if (_audioSrc)
                {
                    player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    player.SetTargetAudioSource(0, _audioSrc);
                }
                else
                {
                    // no AudioSource found - do not play audio  (you can change this to VideoAudioOutputMode.Direct if you want to play the video's raw audio tracks)
                    for (ushort i = 0; i < player.audioTrackCount; i++)
                    {
                        player.audioOutputMode = VideoAudioOutputMode.None;
                    }
                }
                // apply our settings - video will play once preparation is complete via the event handler we set up earlier
                player.Prepare();
            }
            else
            {
                Debug.LogWarning("No video specified on video projector! " + gameObject.name + " is disabled.");
                gameObject.SetActive(false);
            }
        }

        void PlayAfterPrepared()
        {
            pj.enabled = true;
            player.Play();
        }

        /// <summary>
        /// To be used only if you don't want the Projector to play the video immediately (ProjectVideo.playOnAwake = false).
        /// This function plays the video immediately if VideoPlayer preparation has completed. Otherwise, it will play as soon as the preparation has completed.
        /// </summary>
        public void PlayVideoProjector()
        {
            if (player.isPrepared)
            {
                player.Play();
                pj.enabled = true;
            }
            else
            {
                player.prepareCompleted += delegate { PlayAfterPrepared(); };
            }
        }

        public VideoPlayer GetVideoPlayer()
        {
            return player;
        }
    }

}