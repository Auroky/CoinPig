using Godot;

public partial class StaminaMeter : Control
{
    private int _current, _capacity;
    public void SetValue(int current, int capacity)
    {
        _current = current;
        _capacity = capacity;
        QueueRedraw();
    }
    public override void _Draw()
    {
        float width = Mathf.Min(16, (Size.X - 6) / Mathf.Max(1, _capacity));
        for (int i = 0; i < _capacity; i++)
        {
            Color color = i < _current ? WorkshopStyle.Brass : new Color(.8f, .7f, .5f, .12f);
            DrawRect(new(i * width, 0, width - 4, 14), color);
        }
    }
}
