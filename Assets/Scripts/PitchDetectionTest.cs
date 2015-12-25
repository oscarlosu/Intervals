using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Pitch;
using System.Collections.Generic;

public class PitchDetectionTest : MonoBehaviour
{
    public Text IntervalText;
    public Text Pitch1Text;
    public Text Pitch2Text;

    public int SampleFrequency;
    public int SampleLength;

    public float LowestFrequency = 27.5000f;

    public float DetectionStartDelay;
    public float DetectionPeriod;

    public Color c1;
    public Color c2;

    //public float Unison = 1.0f;
    //public float MinorSecond = 1.0594631f;
    //public float MajorSecond = 1.122462f;
    //public float MinorThird = 1.1892071f;
    //public float MajorThird = 1.2377263f;
    //public float PerfectFourth = 1.3348399f;
    //public float AugmentedFourth = 1.4142136f;
    //public float PerfectFifth = 1.4983071f;
    //public float MinorSixth = 1.5874011f;
    //public float MajorSixth = 1.6817928f;
    //public float MinorSeventh = 1.7817974f;
    //public float MajorSeventh = 1.8877486f;
    //public float Octave = 2.0f;

    public List<string> IntervalNames;
    public List<float> IntervalRatios;

    private float maxRatioDistance;

    private AudioClip clip;
    private int lastPosition;
    private int clipSize;
    private float[] buffer;
    private PitchTracker pitchTracker;
    
    private float pitch1;
    private float pitch2;

    private SpriteRenderer rend;
    // Use this for initialization
    void Start()
    {
        // Initialize clip and start taking input from microphone
        clip = new AudioClip();
        clip = Microphone.Start(null, true, SampleLength, SampleFrequency);
        lastPosition = 0;
        clipSize = SampleFrequency * SampleLength;
        // Intialize pitch tracker
        pitchTracker = new PitchTracker();
        pitchTracker.SampleRate = SampleFrequency;
        // Initialize pitches
        pitch1 = 0;
        pitch2 = 0;
        // Calculate max interval ratio distance
        maxRatioDistance = GetMaxRatioDistance();
        // Schedule periodic execution of interval detection
        InvokeRepeating("RegisterInterval", DetectionStartDelay, DetectionPeriod);
        // Get renderer component
        rend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void RegisterInterval()
    {
        // Animate sprite
        rend.color = rend.color == c1 ? c2 : c1;
        float newPitch = GetPitch();
        // Ignore input below the lowest frequency allowed
        if (newPitch < LowestFrequency)
        {
            return;
        }
        //// When the pitches are too low, set both to the new pitch but dont calculate interval
        //if (pitch1 < LowestFrequency && pitch2 < LowestFrequency)
        //{
        //    pitch1 = newPitch;
        //    pitch2 = newPitch;
        //    return;
        //}

        // Register interval
        bool ascending;
        // Calculate frequency ratio between 
        //float ratio = GetRatio(pitch2, newPitch, out ascending);
        // If the ratio is too small, we update only pitch2 (assume fine tuning or unison)
        //if(ratio < MinorSecondMin)
        //{
        //    pitch2 = newPitch;
        //}
        //// If the ratio is big enough, we assume its an interval
        //else
        //{
        //    pitch1 = pitch2;
        //    pitch2 = newPitch;
        //}
        // Calculate new ratio
        pitch1 = pitch2;
        pitch2 = newPitch;
        float ratio = GetRatio(pitch1, pitch2, out ascending);
        // Display results
        Pitch1Text.text = pitch1.ToString();
        Pitch2Text.text = pitch2.ToString();
        IntervalText.text = GetIntervalName(ratio);


    }

    private float GetRatio(float p1, float p2, out bool ascending)
    {
        float ratio;
        // Ascending interval
        if (p2 >= p1)
        {
            ratio = p2 / p1;
            ascending = true;
        }
        // Descending interval
        else
        {
            ratio = p1 / p2;
            ascending = false;
        }
        return ratio;
    }


    private string GetIntervalName(float p1, float p2, out bool ascending)
    {
        float ratio = GetRatio(p1, p2, out ascending);
        return GetIntervalName(ratio);
    }
    private string GetIntervalName(float ratio)
    {
        // Find the minimum distance
        float min = Mathf.Infinity;
        int minIndex = 0;
        //for(int i = 0; i < IntervalRatios.Count; ++i)
        //{
        //    float dist = Mathf.Abs(IntervalRatios[i] - ratio);
        //    if(min > dist)
        //    {
        //        min = dist;
        //        minIndex = i;
        //    }
        //}
        // This formula converts the frequency ratio into semitones
        float semitones = 12 * Mathf.Log(ratio, 2);
        int index = Mathf.RoundToInt(semitones);
        if (index < 0 || index >= IntervalNames.Count)
        {
            return "";
        }
        else
        {
            return IntervalNames[index];
        }
        
    }

    float GetMaxRatioDistance()
    {
        float max = - Mathf.Infinity;
        for (int i = 1; i < IntervalRatios.Count; ++i)
        {
            float dist = Mathf.Abs(IntervalRatios[i] - IntervalRatios[i - 1]);
            if (max < dist)
            {
                max = dist;
            }
        }
        return max;
    }

    private float GetPitch()
    {
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

    private int GetPollSize(int currentPos, int lastPos)
    {
        if (currentPos >= lastPos)
        {
            return currentPos - lastPos;
        }
        else
        {
            return clipSize - lastPos + currentPos;
        }
    }
}
