﻿using Artemis.Core;
using Artemis.Storage.Entities.Profile;
using Artemis.VisualScripting.Nodes.DataModel.Screens;
using Avalonia.Threading;

namespace Artemis.VisualScripting.Nodes.DataModel;

[Node("Data Model-Value", "Outputs a selectable data model value.", "Data Model", OutputType = typeof(object))]
public class DataModelNode : Node<DataModelPathEntity, DataModelNodeCustomViewModel>, IDisposable
{
    private DataModelPath? _dataModelPath;

    public DataModelNode() : base("Data Model", "Outputs a selectable data model value")
    {
        Output = CreateOutputPin(typeof(object));
        StorageModified += (_, _) => UpdateDataModelPath();
    }

    public INodeScript? Script { get; private set; }
    public OutputPin Output { get; }

    public override void Initialize(INodeScript script)
    {
        Script = script;

        if (Storage == null)
            return;

        UpdateDataModelPath();
    }

    public override void Evaluate()
    {
        if (_dataModelPath == null || !_dataModelPath.IsValid)
            return;

        object? pathValue = _dataModelPath.GetValue();
        if (pathValue == null)
        {
            if (!Output.Type.IsValueType)
                Output.Value = null;
        }
        else
        {
            Output.Value = Output.IsNumeric ? new Numeric(pathValue) : pathValue;
        }
    }

    public void UpdateOutputPin()
    {
        Type? type = _dataModelPath?.GetPropertyType();
        if (type == null)
            type = typeof(object);
        else if (Numeric.IsTypeCompatible(type))
            type = typeof(Numeric);

        if (Output.Type != type)
            Output.ChangeType(type);
    }

    private void UpdateDataModelPath()
    {
        DataModelPath? old = _dataModelPath;
        old?.Dispose();

        _dataModelPath = Storage != null ? new DataModelPath(Storage) : null;
        if (_dataModelPath != null)
            _dataModelPath.PathValidated += DataModelPathOnPathValidated;
        UpdateOutputPin();
    }

    private void DataModelPathOnPathValidated(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(UpdateOutputPin);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dataModelPath?.Dispose();
    }
}