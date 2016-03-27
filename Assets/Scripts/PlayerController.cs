using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pitch;

public class PlayerController : MonoBehaviour {
    public List<string> IntervalNames;
    public List<float> IntervalRatios;

    public enum PlayerControllerState {
        FirstNote,
        SecondNote,
    }
    public PlayerControllerState State;
    private bool listening;

    public float Timeout;
    private float elapsedTime;

    // Pitch detection
    public int SampleFrequency;
    public int SampleLength;

    private AudioClip clip;
    private int clipSize;
    private PitchTracker pitchTracker;
    private int lastPosition;
    private float[] buffer;

    public float LastPitch;


    private System.Diagnostics.Stopwatch stopwatch;

    // Use this for initialization
    void Start () {
        InitializePitchDetection();
        listening = true;
        State = PlayerControllerState.FirstNote;
        //InvokeRepeating("FlappyBird", 0.5f, 0.1f);
        //StartCoroutine(FlappyBird());
    }
	
	// Update is called once per frame
	void Update () {
        float newPitch = GetPitch();
        newPitch = (newPitch - 90.0f) / (300.0f - 90.0f);
        float yInc = Mathf.Lerp(-5.0f, 5.0f, newPitch);
        LastPitch = yInc;
        //transform.position = new Vector3(transform.position.x, yInc, 0);
        transform.position = transform.position + Time.deltaTime * yInc * transform.up;
    }

    IEnumerator FlappyBird() {
        yield return new WaitForSeconds(5.0f);
        while(true) {
            float newPitch = GetPitch();
            newPitch = (newPitch - 90.0f) / (300.0f - 90.0f);
            float yInc = Mathf.Lerp(0.0f, 10.0f, newPitch);
            LastPitch = yInc;
            transform.position = new Vector3(transform.position.x, yInc, 0);
            yield return null;
        }        
    }
    void ProcessInterval(float interval, bool ascending) {
        transform.position = transform.position + interval * transform.up;
    }

    void OnTriggerEnter2D(Collider2D other) {
        listening = true;
    }

    void OnTriggerExit2D(Collider2D other) {
        listening = false;
    }
    
    void Listen() {
        if(listening) {
            float newPitch = GetPitch();
            switch (State) {
                case PlayerControllerState.FirstNote:
                    // Register pitch if within range
                    if (newPitch >= PitchTracker.MinDetectFrequency && newPitch <= PitchTracker.MaxDetectFrequency) {
                        // Save pitch
                        LastPitch = newPitch;
                        // Change state
                        State = PlayerControllerState.SecondNote;
                        // Start timer
                        //elapsedTime = System.DateTime.Now.Ticks / 10000000.0f;
                        stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        //Debug.Log(newPitch);
                    }
                    break;
                case PlayerControllerState.SecondNote:
                    // Revert to FirstNote state after Timeout seconds of silence
                    if (stopwatch.Elapsed.Seconds > Timeout) {
                        stopwatch.Stop();
                        State = PlayerControllerState.FirstNote;
                    } else if (newPitch >= PitchTracker.MinDetectFrequency && newPitch <= PitchTracker.MaxDetectFrequency) {
                        // Change state
                        State = PlayerControllerState.FirstNote;
                        // Calculate interval
                        bool ascending;
                        float interval = GetInterval(LastPitch, newPitch, out ascending);
                        // Convert to number of tones
                        interval = IntervalToTones(interval);
                        // Process interval
                        ProcessInterval(interval, ascending);
                        
                    }
                    break;
            }
        }             
    }

    private float GetInterval(float p1, float p2, out bool ascending) {
        float ratio;
        // Ascending interval
        if (p2 >= p1) {
            ratio = p2 / p1;
            ascending = true;
        }
        // Descending interval
        else {
            ratio = p1 / p2;
            ascending = false;
        }
        return ratio;
    }
    private float IntervalToTones(float interval) {
        float tones = 6.0f * Mathf.Log(interval, 2);
        return tones;
    }



    // Pitch detection
    private void InitializePitchDetection() {
        // Initialize clip and start taking input from microphone
        clip = new AudioClip();
        clip = Microphone.Start(null, true, SampleLength, SampleFrequency);
        lastPosition = 0;
        clipSize = SampleFrequency * SampleLength;
        // Intialize pitch tracker
        pitchTracker = new PitchTracker();
        pitchTracker.SampleRate = SampleFrequency;
        // Schedule periodic execution of interval detection
        //InvokeRepeating("RegisterInterval", DetectionStartDelay, DetectionPeriod);
    }
    private float GetPitch() {
        // Get current position
        int currentPosition = Microphone.GetPosition(null);
        // Calculate poll size and reserve memory
        int pollSize = GetPollSize(currentPosition, lastPosition);
        buffer = new float[pollSize];
        // Poll the data from the audio clip
        clip.GetData(buffer, lastPosition);
        // Update last position
        lastPosition = currentPosition;

        // Get pitch
        pitchTracker.ProcessBuffer(buffer);
        return pitchTracker.CurrentPitchRecord.Pitch;
    }

    private int GetPollSize(int currentPos, int lastPos) {
        if (currentPos >= lastPos) {
            return currentPos - lastPos;
        } else {
            return clipSize - lastPos + currentPos;
        }
    }

}
