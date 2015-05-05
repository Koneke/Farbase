namespace Farbase
{
    public interface ISelection
    {
        Vector2i GetSelection();
    }

    public class TileSelection : ISelection
    {
        private Tile selected;

        public TileSelection(Tile t)
        {
            selected = t;
        }

        public Vector2i GetSelection()
        {
            return new Vector2i(
                selected.Position.X,
                selected.Position.Y
            );
        }
    }

    public class UnitSelection : ISelection
    {
        private Unit selected;

        public UnitSelection(Unit unit)
        {
            selected = unit;
        }

        public Vector2i GetSelection()
        {
            return selected.Position;
        }
    }

}