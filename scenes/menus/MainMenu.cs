using Godot;

namespace Fragile
{
    public class MainMenu : BaseMenu
    {
        private TextureRect logo;
        float count = 0;

        public override void _Ready()
        {
            base._Ready();

            int masterIdx = AudioServer.GetBusIndex("Master");
            int musicIdx = AudioServer.GetBusIndex("Music");
            int sfxIdx = AudioServer.GetBusIndex("SFX");

            HSlider master = GetNode<HSlider>("MasterVolSlider");
            HSlider music = GetNode<HSlider>("MusicVolSlider");
            HSlider sfx = GetNode<HSlider>("SFXVolSlider");
            CheckButton fullscreen = GetNode<CheckButton>("FullScreenCheck");

            master.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(masterIdx));
            music.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(musicIdx));
            sfx.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(sfxIdx));
            fullscreen.Pressed = OS.WindowFullscreen;

            master.Connect("value_changed", this, nameof(VolSliderChanged), new Godot.Collections.Array() { masterIdx });
            music.Connect("value_changed", this, nameof(VolSliderChanged), new Godot.Collections.Array() { musicIdx });
            sfx.Connect("value_changed", this, nameof(VolSliderChanged), new Godot.Collections.Array() { sfxIdx });
            fullscreen.Connect("toggled", this, nameof(SetFullscreen));

            master.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
            music.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
            sfx.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
            fullscreen.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));

            GetNode<Label>("PersonalBestLabel").Text = $"Personal Best: {DriveWorld.FormatDistance(SaveData.MaxDistance)}";

            logo = GetNode<TextureRect>("Logo");

            GetNode("Credit").Connect("meta_clicked", this, nameof(CreditMetaClicked));
        }

        private void VolSliderChanged(float linearVol, int busIdx)
        {
            AudioServer.SetBusVolumeDb(busIdx, GD.Linear2Db(linearVol));
        }

        private void SetFullscreen(bool fullscreen)
        {
            OS.WindowFullscreen = fullscreen;
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            count += delta * .52083f;
            if (count >= 1f)
                count--;

            float t = count * Mathf.Tau;
            float a = Mathf.Cos(t);

            logo.RectRotation = 7.5f * a;
        }

        private void CreditMetaClicked(object meta)
        {
            if (meta is string str)
            {
                OS.ShellOpen(str);
            }
        }
    }
}