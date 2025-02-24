﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Artemis.Core;

/// <summary>
///     Represents a kind of node inside a <see cref="INodeScript" />
/// </summary>
public interface INode : INotifyPropertyChanged
{
    /// <summary>
    ///     Gets or sets the ID of the node.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    ///     Gets the name of the node
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the description of the node
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Gets a boolean indicating whether the node is the exit node of the script
    /// </summary>
    bool IsExitNode { get; }

    /// <summary>
    ///     Gets a boolean indicating whether the node is a default node that connot be removed
    /// </summary>
    bool IsDefaultNode { get; }

    /// <summary>
    ///     Gets or sets the X-position of the node
    /// </summary>
    public double X { get; set; }

    /// <summary>
    ///     Gets or sets the Y-position of the node
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    ///     Gets a read-only collection of the pins on this node
    /// </summary>
    public IReadOnlyCollection<IPin> Pins { get; }

    /// <summary>
    ///     Gets a read-only collection of the pin collections on this node
    /// </summary>
    public IReadOnlyCollection<IPinCollection> PinCollections { get; }

    /// <summary>
    ///     Called when the node resets
    /// </summary>
    event EventHandler Resetting;

    /// <summary>
    ///     Called when the node was loaded from storage or newly created
    /// </summary>
    /// <param name="script">The script the node is contained in</param>
    void Initialize(INodeScript script);

    /// <summary>
    ///     Evaluates the value of the output pins of this node
    /// </summary>
    void Evaluate();

    /// <summary>
    ///     Resets the node causing all pins to re-evaluate the next time <see cref="Evaluate" /> is called
    /// </summary>
    void Reset();
}