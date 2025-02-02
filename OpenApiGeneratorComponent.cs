﻿using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace GlooGenPlugin;

[Category(new string[] { "Network/OpenApi" })]
public class OpenApiGenerator : Component, ICustomInspector
{
    public readonly SyncRef<Slot> TargetModelSlot;
    public readonly SyncRef<IField<string>> OpenApiStringField;

    protected override void OnAttach()
    {
        base.OnAttach();
        Slot modelSlot = Slot.AddSlot("Models");
        Slot specFieldSlot = Slot.AddSlot("Spec Field");

        specFieldSlot.SetParent(this.Slot);
        modelSlot.SetParent(this.Slot);

        ValueField<string> specField = specFieldSlot.AttachComponent<ValueField<string>>();
        Comment specFieldComment = specFieldSlot.AttachComponent<Comment>();
        specFieldComment.Text.Value = "Paste OpenAPI JSON/YAML in above field. Only single doc supported.";
        OpenApiStringField.Target = specField.Value;

        TargetModelSlot.Target = modelSlot;
    }

    public void BuildInspectorUI(UIBuilder ui)
    {
        string glooGenVersion = this.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        glooGenVersion = glooGenVersion.Substring(0, glooGenVersion.IndexOf('+'));
        WorkerInspector.BuildInspectorUI(this, ui);
		ui.HorizontalLayout(4f);
		LocaleString text = "Generate Model Slots";
		ui.Button(in text, GenerateModelsPressed);
		ui.NestOut();

        // Print out some debug/version info.
        text = "------------------";
        ui.Text(in text);
        text = $"GlooGen ver: {glooGenVersion}";
        ui.Text(in text);
    }

    [SyncMethod(typeof(Action), new string[] { })]
    public void GenerateModelsPressed()
    {
        var specReader = new OpenApiStringReader();
        var specDiag = new OpenApiDiagnostic();
        string doc = (string)OpenApiStringField.Target.BoxedValue;
        OpenApiDocument parsedSpec = specReader.Read(doc, out specDiag);

        foreach(var schema in parsedSpec.Components.Schemas)
        {
            var _slot = Slot.AddSlot();
            _slot.Name = schema.Key;
            _slot.SetParent(TargetModelSlot.Target);
            var _dynVarSpace = _slot.AttachComponent<DynamicVariableSpace>();
            _dynVarSpace.SpaceName.Value = schema.Key;

            if(schema.Value.Type == "object" && schema.Value.Properties.Count > 0)
            {
                foreach(var schemaProperty in schema.Value.Properties)
                {
                    AttachVariable(_slot, schemaProperty);
                }
            }
            else
            {
                AttachVariable(_slot, schema);
            }
        }

        // For any property that's an object, we should try to populate any ".template" variable referencing the appropriate model slot.
        List<DynamicReferenceVariable<Slot>> templateVars = TargetModelSlot.Target.GetComponentsInChildren<DynamicReferenceVariable<Slot>>()
            .Where( s => s.VariableName.Value.EndsWith(".template"))
            .ToList();
        
        // TODO: Make this better. Find a nice way to know ahead of time if a template slot exists for a parsed object.
        // Right now we are creating a .template DynamicReferenceVariable<Slot> preemptively. This will check if a slot
        // exists and destroys the .template DynamicReferenceVariable<Slot> if we don't need it.
        foreach(DynamicReferenceVariable<Slot> templateVar in templateVars)
        {
            string _objectTemplateName = templateVar.VariableName.Value.Substring(0, templateVar.VariableName.Value.Length - ".template".Length);
            if(parsedSpec.Components.Schemas.ContainsKey(_objectTemplateName))
            {
                templateVar.Reference.Target = TargetModelSlot.Target.FindChild(_objectTemplateName);
            }
            else{
                UniLog.Warning($"[GlooGen] Could not find template slot for object {_objectTemplateName}.");
                templateVar.Destroy();
            }
        }

    }

    [SyncMethod(typeof(Delegate), null)]
    private void GenerateModelsPressed(IButton button, ButtonEventData eventData)
    {
        GenerateModelsPressed();
    }

    /// <summary>
    /// Parses and attaches appropriatly-typed Dynamic Variables based on types parsed from OpenApi Spec.
    /// </summary>
    /// <seealso cref="https://swagger.io/docs/specification/data-models/data-types/"/>
    /// <param name="targetSlot"><c>FrooxEngine.Slot</c> that the Dynamic Variable will attach to.</param>
    /// <param name="schemaProperty">Invididual <c>OpenApiSchema</c> containing parsed type definition.</param>
    private void AttachVariable(Slot targetSlot, KeyValuePair<string,OpenApiSchema?> schemaProperty)
    {
        // Using reflection to get the DynamicVariableHelper.CreateVariable<T>.
        // Once we know the type, we can set the type and invoke the method to create our variables.
        MethodInfo dynVarHelperMethod;
        string helperMethodName = "CreateVariable";
        BindingFlags helperBindingFlags = BindingFlags.Public | BindingFlags.Static;

        // We check if there's a more specific format before parsing to a basic type.
        if(String.IsNullOrEmpty(schemaProperty.Value.Format))
        {
            switch(schemaProperty.Value.Type.ToLower())
            {
                case("string"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(string)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("number"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(float)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("integer"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(int)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("boolean"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(bool)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("object"):
                    if(schemaProperty.Value.AdditionalProperties != null || schemaProperty.Value.AdditionalProperties?.Properties.Count > 0)
                    {

                        // Recursively call AttachVariable for objects that have additional properties.
                        AttachVariable(
                            targetSlot, 
                            new KeyValuePair<string, OpenApiSchema?>(schemaProperty.Key, schemaProperty.Value.AdditionalProperties)
                        );
                        break;
                    }
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(Slot)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(Slot)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, $"{schemaProperty.Value.Reference.Id}.template", null, true]);
                    break;
                case("array"):
                    Slot arraySlot = Slot.AddSlot($"{schemaProperty.Key}[]");
                    arraySlot.SetParent(targetSlot, false);
                    arraySlot.Tag = String.IsNullOrEmpty(schemaProperty.Value.Items.Format) ? schemaProperty.Value.Items.Type : schemaProperty.Value.Items.Format;

                    if(schemaProperty.Value.Items.Type == "object" && schemaProperty.Value.Items.Reference != null)
                    {
                        arraySlot.Tag = schemaProperty.Value.Items.Reference.Id;
                        dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                            helperMethodName,
                            helperBindingFlags,
                            typeof(Slot)
                        );
                        dynVarHelperMethod.Invoke(null, [targetSlot, $"{schemaProperty.Value.Items.Reference.Id}.template", null, true]);
                    }
                    break;
                default:
                    break;
            }
        }
        else 
        {
            switch(schemaProperty.Value.Format.ToLower())
            {
                case("float"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(float)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("double"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(double)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                // FrooxEngine doesn't differentiate between 32-bit and 64-bit ints.
                // Only Signed or Unsigned.
                case("int32"):
                case("int64"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(int)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                // Roll all date and/or time values into the one DateTime type.
                case("date"):
                case("time"):
                case("date-time"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(DateTime)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                case("duration"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(TimeSpan)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                // This isn't a standard format but could come up commonly.
                // Sometimes specs will just have URLs be basic strings anyway.
                case("uri"):
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(Uri)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
                default:
                    // For any type we're unsure of we'll do a string for now.
                    // Assuming it's a free-form format that's undefined.
                    // Examples include "email" or "uuid", etc.
                    // See: https://swagger.io/docs/specification/data-models/data-types/#string
                    dynVarHelperMethod = typeof(DynamicVariableHelper).GetGenericMethod(
                        helperMethodName,
                        helperBindingFlags,
                        typeof(string)
                    );
                    dynVarHelperMethod.Invoke(null, [targetSlot, schemaProperty.Key, null, true]);
                    break;
            }
        }
    }

}
