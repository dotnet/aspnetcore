// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Supplies information about an event that is being raised.
    /// </summary>
    public class UIEventArgs
    {
        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Supplies information about an input change event that is being raised.
    /// </summary>
    public class UIChangeEventArgs : UIEventArgs
    {
        /// <summary>
        /// Gets or sets the new value of the input. This may be a <see cref="string"/>
        /// or a <see cref="bool"/>.
        /// </summary>
        public object Value { get; set; }
    }

    /// <summary>
    /// Supplies information about an clipboard event that is being raised.
    /// </summary>
    public class UIClipboardEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about an drag event that is being raised.
    /// </summary>
    public class UIDragEventArgs : UIEventArgs
    {
        /// <summary>
        /// A count of consecutive clicks that happened in a short amount of time, incremented by one.
        /// </summary>
        public long Detail { get; set; }

        /// <summary>
        /// The data that underlies a drag-and-drop operation, known as the drag data store.
        /// See <see cref="DataTransfer"/>.
        /// </summary>
        public DataTransfer DataTransfer { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public long ScreenX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public long ScreenY { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public long ClientX { get; set; }

        /// <summary>
        /// 	The Y coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public long ClientY { get; set; }

        /// <summary>
        /// The button number that was pressed when the mouse event was fired:
        /// Left button=0,
        /// middle button=1 (if present),
        /// right button=2.
        /// For mice configured for left handed use in which the button actions are reversed the values are instead read from right to left.
        /// </summary>
        public long Button { get; set; }

        /// <summary>
        /// The buttons being pressed when the mouse event was fired:
        /// Left button=1,
        /// Right button=2,
        /// Middle (wheel) button=4,
        /// 4th button (typically, "Browser Back" button)=8,
        /// 5th button (typically, "Browser Forward" button)=16.
        /// If two or more buttons are pressed, returns the logical sum of the values.
        /// E.g., if Left button and Right button are pressed, returns 3 (=1 | 2).
        /// </summary>
        public long Buttons { get; set; }
        
        /// <summary>
        /// true if the control key was down when the event was fired. false otherwise.
        /// </summary>
        public bool CtrlKey { get; set; }

        /// <summary>
        /// true if the shift key was down when the event was fired. false otherwise.
        /// </summary>
        public bool ShiftKey { get; set; }

        /// <summary>
        /// true if the alt key was down when the event was fired. false otherwise.
        /// </summary>
        public bool AltKey { get; set; }

        /// <summary>
        /// true if the meta key was down when the event was fired. false otherwise.
        /// </summary>
        public bool MetaKey { get; set; }
    }

    /// <summary>
    /// The <see cref="DataTransfer"/> object is used to hold the data that is being dragged during a drag and drop operation.
    /// It may hold one or more <see cref="UIDataTransferItem"/>, each of one or more data types.
    /// For more information about drag and drop, see HTML Drag and Drop API.
    /// </summary>
    public class DataTransfer
    {
        /// <summary>
        /// Gets the type of drag-and-drop operation currently selected or sets the operation to a new type.
        /// The value must be none, copy, link or move.
        /// </summary>
        public string DropEffect { get; set; }

        /// <summary>
        /// Provides all of the types of operations that are possible.
        /// Must be one of none, copy, copyLink, copyMove, link, linkMove, move, all or uninitialized.
        /// </summary>
        public string EffectAllowed { get; set; }

        /// <summary>
        /// Contains a list of all the local files available on the data transfer.
        /// If the drag operation doesn't involve dragging files, this property is an empty list.
        /// </summary>
        public string[] Files { get; set; }

        /// <summary>
        /// Gives a <see cref="UIDataTransferItem"/> array which is a list of all of the drag data.
        /// </summary>
        public UIDataTransferItem[] Items { get; set; }

        /// <summary>
        /// An array of <see cref="string"/> giving the formats that were set in the dragstart event.
        /// </summary>
        public string[] Types { get; set; }
    }

    /// <summary>
    /// The <see cref="UIDataTransferItem"/> object represents one drag data item.
    /// During a drag operation, each drag event has a dataTransfer property which contains a list of drag data items.
    /// Each item in the list is a <see cref="UIDataTransferItem"/> object.
    /// </summary>
    public class UIDataTransferItem
    {
        /// <summary>
        /// The kind of drag data item, string or file
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// The drag data item's type, typically a MIME type
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Supplies information about an error event that is being raised.
    /// </summary>
    public class UIErrorEventArgs : UIEventArgs
    {
        /// <summary>
        /// Gets a a human-readable error message describing the problem.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the name of the script file in which the error occurred.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets the line number of the script file on which the error occurred.
        /// </summary>
        public int Lineno { get; set; }

        /// <summary>
        /// Gets the column number of the script file on which the error occurred.
        /// </summary>
        public int Colno { get; set; }
    }

    /// <summary>
    /// Supplies information about a focus event that is being raised.
    /// </summary>
    public class UIFocusEventArgs : UIEventArgs
    {
        // Not including support for 'relatedTarget' since we don't have a good way to represent it.
        // see: https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent
    }

    /// <summary>
    /// Supplies information about a keyboard event that is being raised.
    /// </summary>
    public class UIKeyboardEventArgs : UIEventArgs
    {
        /// <summary>
        /// The key value of the key represented by the event. 
        /// If the value has a printed representation, this attribute's value is the same as the char attribute. 
        /// Otherwise, it's one of the key value strings specified in 'Key values'. 
        /// If the key can't be identified, this is the string "Unidentified"
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Holds a string that identifies the physical key being pressed. 
        /// The value is not affected by the current keyboard layout or modifier state, so a particular key will always return the same value.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The location of the key on the device.
        /// </summary>
        public float Location { get; set; }

        /// <summary>
        /// true if a key has been depressed long enough to trigger key repetition, otherwise false.
        /// </summary>
        public bool Repeat { get; set; }

        /// <summary>
        /// true if the control key was down when the event was fired. false otherwise.
        /// </summary>
        public bool CtrlKey { get; set; }

        /// <summary>
        /// true if the shift key was down when the event was fired. false otherwise.
        /// </summary>
        public bool ShiftKey { get; set; }

        /// <summary>
        /// true if the alt key was down when the event was fired. false otherwise.
        /// </summary>
        public bool AltKey { get; set; }

        /// <summary>
        /// true if the meta key was down when the event was fired. false otherwise.
        /// </summary>
        public bool MetaKey { get; set; }
    }

    /// <summary>
    /// Supplies information about a mouse event that is being raised.
    /// </summary>
    public class UIMouseEventArgs : UIEventArgs
    {
        /// <summary>
        /// A count of consecutive clicks that happened in a short amount of time, incremented by one.
        /// </summary>
        public long Detail { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public long ScreenX { get; set; }

        /// <summary>
        /// The Y coordinate of the mouse pointer in global (screen) coordinates.
        /// </summary>
        public long ScreenY { get; set; }

        /// <summary>
        /// The X coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public long ClientX { get; set; }

        /// <summary>
        /// 	The Y coordinate of the mouse pointer in local (DOM content) coordinates.
        /// </summary>
        public long ClientY { get; set; }

        /// <summary>
        /// The button number that was pressed when the mouse event was fired:
        /// Left button=0,
        /// middle button=1 (if present),
        /// right button=2.
        /// For mice configured for left handed use in which the button actions are reversed the values are instead read from right to left.
        /// </summary>
        public long Button { get; set; }

        /// <summary>
        /// The buttons being pressed when the mouse event was fired:
        /// Left button=1,
        /// Right button=2,
        /// Middle (wheel) button=4,
        /// 4th button (typically, "Browser Back" button)=8,
        /// 5th button (typically, "Browser Forward" button)=16.
        /// If two or more buttons are pressed, returns the logical sum of the values.
        /// E.g., if Left button and Right button are pressed, returns 3 (=1 | 2).
        /// </summary>
        public long Buttons { get; set; }

        /// <summary>
        /// true if the control key was down when the event was fired. false otherwise.
        /// </summary>
        public bool CtrlKey { get; set; }

        /// <summary>
        /// true if the shift key was down when the event was fired. false otherwise.
        /// </summary>
        public bool ShiftKey { get; set; }

        /// <summary>
        /// true if the alt key was down when the event was fired. false otherwise.
        /// </summary>
        public bool AltKey { get; set; }

        /// <summary>
        /// true if the meta key was down when the event was fired. false otherwise.
        /// </summary>
        public bool MetaKey { get; set; }
    }

    /// <summary>
    /// Supplies information about a mouse event that is being raised.
    /// </summary>
    public class UIPointerEventArgs : UIMouseEventArgs
    {
        /// <summary>
        /// A unique identifier for the pointer causing the event.
        /// </summary>
        public string PointerId { get; set; }

        /// <summary>
        /// The width (magnitude on the X axis), in CSS pixels, of the contact geometry of the pointer.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// The height (magnitude on the Y axis), in CSS pixels, of the contact geometry of the pointer.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// The normalized pressure of the pointer input in the range of 0 to 1,
        /// where 0 and 1 represent the minimum and maximum pressure the hardware is capable of detecting, respectively.
        /// </summary>
        public float Pressure { get; set; }

        /// <summary>
        /// The plane angle (in degrees, in the range of -90 to 90) between the Y-Z plane
        /// and the plane containing both the transducer (e.g. pen stylus) axis and the Y axis.
        /// </summary>
        public float TiltX { get; set; }

        /// <summary>
        /// The plane angle (in degrees, in the range of -90 to 90) between the X-Z plane
        /// and the plane containing both the transducer (e.g. pen stylus) axis and the X axis.
        /// </summary>
        public float TiltY { get; set; }

        /// <summary>
        /// Indicates the device type that caused the event.
        /// Must be one of the strings mouse, pen or touch, or an empty string.
        /// </summary>
        public string PointerType { get; set; }

        /// <summary>
        /// Indicates if the pointer represents the primary pointer of this pointer type.
        /// </summary>
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Supplies information about a progress event that is being raised.
    /// </summary>
    public class UIProgressEventArgs : UIEventArgs
    {
        /// <summary>
        /// Whether or not the total size of the transfer is known.
        /// </summary>
        public bool LengthComputable { get; set; }

        /// <summary>
        /// The number of bytes transferred since the beginning of the operation.
        /// This doesn't include headers and other overhead, but only the content itself.
        /// </summary>
        public long Loaded { get; set; }

        /// <summary>
        /// The total number of bytes of content that will be transferred during the operation.
        /// If the total size is unknown, this value is zero. 
        /// </summary>
        public long Total { get; set; }
    }

    /// <summary>
    /// Supplies information about a touch event that is being raised.
    /// </summary>
    public class UITouchEventArgs : UIEventArgs
    {
        /// <summary>
        /// A count of consecutive clicks that happened in a short amount of time, incremented by one.
        /// </summary>
        public long Detail { get; set; }

        /// <summary>
        /// A list of <see cref="UITouchPoint"/> for every point of contact currently touching the surface.
        /// </summary>
        public UITouchPoint[] Touches { get; set; }

        /// <summary>
        /// A list of <see cref="UITouchPoint"/> for every point of contact that is touching the surface and started on the element that is the target of the current event.
        /// </summary>
        public UITouchPoint[] TargetTouches { get; set; }

        /// <summary>
        /// A list of Touches for every point of contact which contributed to the event.
        /// For the touchstart event this must be a list of the touch points that just became active with the current event.
        /// For the touchmove event this must be a list of the touch points that have moved since the last event.
        /// For the touchend and touchcancel events this must be a list of the touch points that have just been removed from the surface.
        /// </summary>
        public UITouchPoint[] ChangedTouches { get; set; }

        /// <summary>
        /// true if the control key was down when the event was fired. false otherwise.
        /// </summary>
        public bool CtrlKey { get; set; }

        /// <summary>
        /// true if the shift key was down when the event was fired. false otherwise.
        /// </summary>
        public bool ShiftKey { get; set; }

        /// <summary>
        /// true if the alt key was down when the event was fired. false otherwise.
        /// </summary>
        public bool AltKey { get; set; }

        /// <summary>
        /// true if the meta key was down when the event was fired. false otherwise.
        /// </summary>
        public bool MetaKey { get; set; }
    }

    /// <summary>
    /// Represents a single contact point on a touch-sensitive device.
    /// The contact point is commonly a finger or stylus and the device may be a touchscreen or trackpad.
    /// </summary>
    public class UITouchPoint
    {
        /// <summary>
        /// A unique identifier for this Touch object.
        /// A given touch point (say, by a finger) will have the same identifier for the duration of its movement around the surface.
        /// This lets you ensure that you're tracking the same touch all the time.
        /// </summary>
        public long Identifier { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the screen.
        /// </summary>
        public long ScreenX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top edge of the screen.
        /// </summary>
        public long ScreenY { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the browser viewport, not including any scroll offset.
        /// </summary>
        public long ClientX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top edge of the browser viewport, not including any scroll offset.
        /// </summary>
        public long ClientY { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the document.
        /// Unlike <see cref="ClientX"/>, this value includes the horizontal scroll offset, if any.
        /// </summary>
        public long PageX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top of the document.
        /// Unlike <see cref="ClientY"/>, this value includes the vertical scroll offset, if any.
        /// </summary>
        public long PageY { get; set; }
    }

    /// <summary>
    /// Supplies information about a mouse wheel event that is being raised.
    /// </summary>
    public class UIWheelEventArgs : UIMouseEventArgs
    {
        /// <summary>
        /// The horizontal scroll amount.
        /// </summary>
        public double DeltaX { get; set; }

        /// <summary>
        /// The vertical scroll amount.
        /// </summary>
        public double DeltaY { get; set; }

        /// <summary>
        /// The scroll amount for the z-axis.
        /// </summary>
        public double DeltaZ { get; set; }

        /// <summary>
        /// The unit of the delta values scroll amount. 
        /// </summary>
        public long DeltaMode { get; set; }
    }
}
