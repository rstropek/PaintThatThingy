#region Global state
// ================================================================================================
// GLOBAL STATE
// ================================================================================================

// Currently selected drawing color
var selectedColor = RED;

// Currently selected drawing shape type
// Note: If this variable is null, user is in "pick" mode. That means
//       that she does not want to draw a new shape but pick an existing
//       one by clicking on it using the mouse.
ShapeType? selectedShapeType = ShapeType.Rectangle;

// Shape that the user is currently drawing
// Note: If this variable is null, the user is currently not in the process
//       of drawing a new shape.
ColoredShape? drawingShape = null;

// Currently selected shape
// Note: If this variable is null, the user has not selected any shape
ColoredShape? selectedShape = null;

// Collection of all created shapes
var shapes = new List<ColoredShape>();

// Create window and set max. frames-per-second
InitWindow(800, 450, "Paint-That-Thingy");
SetTargetFPS(60);
#endregion

#region Main drawing loop
// ================================================================================================
// DRAWING LOOP
// ================================================================================================

while (!WindowShouldClose())
{
    // Check if the user wants to change the color
    if (IsKeyPressed(KeyboardKey.KEY_R)) selectedColor = RED;
    else if (IsKeyPressed(KeyboardKey.KEY_G)) selectedColor = GREEN;
    else if (IsKeyPressed(KeyboardKey.KEY_B)) selectedColor = BLUE;

    // Check if the user wants to change the shape type or switch to "pick" mode
    var oldSelectedShapeType = selectedShapeType;
    if (IsKeyPressed(KeyboardKey.KEY_S)) selectedShapeType = ShapeType.Rectangle;
    else if (IsKeyPressed(KeyboardKey.KEY_C)) selectedShapeType = ShapeType.Circle;
    else if (IsKeyPressed(KeyboardKey.KEY_P)) selectedShapeType = null;

    // If the user change the shape type, we want to remove shape selection
    if (oldSelectedShapeType != selectedShapeType) selectedShape = null;

    // Check if the user wants to delete the currently selected shape
    if (IsKeyPressed(KeyboardKey.KEY_DELETE) && selectedShape != null)
    {
        shapes.Remove(selectedShape);
        selectedShape = null;
    }

    if (drawingShape == null)
    {
        // User is currently not draing a new shape. Did she click
        // with the left mouse button?
        if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON))
        {
            if (selectedShapeType != null)
            {
                // User clicked with the left mouse button and a shape type
                // has been selected. Therefore, we need to create a new shape
                // and position its left upper corner to the position of the mouse.
                drawingShape = ColoredShape.Create(selectedShapeType.Value);
                drawingShape.LeftUpper = GetMousePosition();
                drawingShape.Color = selectedColor;
            }
            else
            {
                // User clicked with the left mouse button and is in "pick" mode.
                // Therefore, we have to check whether there is a shape on the position
                // of the mouse. Note that we search from back of our shape list as
                // the shapes at the end were inserted last. They are visually on the top.
                var mousePos = GetMousePosition();
                selectedShape = shapes.LastOrDefault(s => s.IsPointInside(mousePos));
            }
        }
    }
    else
    {
        // User is currently drawing a shape
        if (IsMouseButtonDown(MOUSE_LEFT_BUTTON))
        {
            // Mouse is still clicked, so we need to change the new
            // shape's right lower corner.
            drawingShape.RightLower = GetMousePosition();
        }
        else
        {
            // Mouse is no longer clicked, so currently drawn shape has
            // to be added to our list of shapes
            shapes.Add(drawingShape);
            drawingShape = null;
        }
    }

    // We start drawing
    BeginDrawing();

    // Clear everything
    ClearBackground(RAYWHITE);

    // Draw all shapes in our shape collection
    foreach (var shape in shapes) shape.Draw();

    // If a shape is selected, draw a selection mark around it
    selectedShape?.DrawSelectionMark();

    // If the user is currently creating a new shape, draw it
    drawingShape?.Draw();

    // And we are done with drawing
    EndDrawing();
}

CloseWindow();
#endregion

#region Types
// ================================================================================================
// TYPES
// ================================================================================================

/// <summary>
/// List of available shapes
/// </summary>
public enum ShapeType
{
    /// <summary>
    /// Represents <see cref="ColoredRectangle"/>
    /// </summary>
    Rectangle,

    /// <summary>
    /// Represents <see cref="ColoredCircle"/>
    /// </summary>
    Circle,
}

/// <summary>
/// Represents a shape
/// </summary>
abstract class ColoredShape
{
    /// <summary>
    /// Draw the shape on the screen
    /// </summary>
    public abstract void Draw();

    /// <summary>
    /// Draw the selection mark around the shape
    /// </summary>
    public abstract void DrawSelectionMark();

    /// <summary>
    /// Check whether the given point is inside the shape
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>
    /// <c>true</c> if the point is inside the shape.
    /// </returns>
    public abstract bool IsPointInside(Vector2 point);

    /// <summary>
    /// Creates a shape
    /// </summary>
    /// <param name="shapeType">Type of shape to create</param>
    /// <returns>
    /// Created shape
    /// </returns>
    public static ColoredShape Create(ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Rectangle => new ColoredRectangle(),
            ShapeType.Circle => new ColoredCircle(),
            _ => throw new ArgumentOutOfRangeException(nameof(shapeType)),
        };
    }

    /// <summary>
    /// Gets or sets the left upper corner
    /// </summary>
    public Vector2 LeftUpper { get; set; } = new Vector2(0, 0);

    /// <summary>
    /// Gets or sets the size
    /// </summary>
    public Vector2 Size { get; set; } = new Vector2(0, 0);

    /// <summary>
    /// Gets or sets the color
    /// </summary>
    public Color Color { get; set; } = BLACK;

    /// <summary>
    /// Gets or sets the right lower corner
    /// </summary>
    public Vector2 RightLower
    {
        get => LeftUpper + Size;
        set => Size = value - LeftUpper;
    }
}

/// <summary>
/// Represents a rectangular shape
/// </summary>
class ColoredRectangle : ColoredShape
{
    /// <inheritdoc/>
    public override void Draw() => DrawRectangleV(LeftUpper, Size, Color);

    /// <inheritdoc/>
    public override void DrawSelectionMark()
        => DrawRectangleLinesEx(new(LeftUpper.X, LeftUpper.Y, Size.X, Size.Y), 5, DARKGRAY);

    /// <inheritdoc/>
    public override bool IsPointInside(Vector2 point)
        => CheckCollisionPointRec(point, new(LeftUpper.X, LeftUpper.Y, Size.X, Size.Y));
}

/// <summary>
/// Represents a circle shape
/// </summary>
class ColoredCircle : ColoredShape
{
    /// <inheritdoc/>
    public override void Draw() => DrawCircleV(Center, Radius, Color);

    /// <inheritdoc/>
    public override void DrawSelectionMark()
    {
        // There is no function to draw a circle with a line width > 1px.
        // Therefore we have to draw multiple circle lines to get a thicker
        // line.

        var center = Center;
        var radius = Radius;
        for (var i = 0; i < 10; i++)
        {
            DrawCircleLines((int)center.X, (int)center.Y, radius - 0.5f * i, DARKGRAY);
        }
    }

    /// <inheritdoc/>
    public override bool IsPointInside(Vector2 point)
        => CheckCollisionPointCircle(point, Center, Radius);

    public Vector2 Center => LeftUpper + Size / 2;

    public float Radius => Size.X / 2;
}
#endregion
