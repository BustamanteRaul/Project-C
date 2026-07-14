namespace MySharpVenture.Pantallas;

public interface IPantalla
{
    // Renders the screen, handles input, and returns which screen to go to next.
    PantallaId Show();
}
