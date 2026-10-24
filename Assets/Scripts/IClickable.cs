public interface IClickable
{
    void OnPress();
    void OnRelease();

    // Most things can't be clicked while the game is paused (e.g. at a pitstop). Shop buttons override this.
    bool WorksWhilePaused => false;
}
