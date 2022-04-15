using Godot;

namespace Fragile
{
    public class EngineSoundPlayer : AudioStreamPlayer
    {
        private static AudioStreamSample start = GD.Load<AudioStreamSample>("res://audio/engine_start.wav");
        private static AudioStreamSample loop = GD.Load<AudioStreamSample>("res://audio/engine_loop.wav");
        private static AudioStreamSample end = GD.Load<AudioStreamSample>("res://audio/engine_finish.wav");

        private float countdown = 0f;

        public override void _Ready()
        {
            base._Ready();

            Bus = "Engine";
            VolumeDb = GD.Linear2Db(.5f);
        }

        public override void _Process(float delta)
        {
            if (countdown > 0f)
            {
                countdown -= delta;

                if (countdown <= 0f && Stream == start)
                {
                    Stream = loop;
                    Play();

                    countdown = 0f;
                }
            }
        }

        public void StartEngine()
        {
            if (Stream == end || !Playing)
            {
                Stream = start;

                Play();

                countdown = 6.5f;
            }
        }

        public void StopEngine()
        {
            if (Stream == loop || (Stream == start && countdown < 3f))
            {
                countdown = 0;

                Stream = end;
                Play();
            }
            else
            {
                Stop();

                countdown = 0;
            }
        }
    }
}