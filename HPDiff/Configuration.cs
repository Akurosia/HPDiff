using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace HPDiff;

[Serializable]
public class Configuration : IPluginConfiguration
{
	public bool mLockGaugeWindow = false;
	public bool mGaugeClickthrough = false;
	public Vector4 mIndicatorColor = Vector4.One;
	public Vector4 mIndicatorWarningColor = new( 255f/255f, 179f/255f, 0f/255f, 1f );
	public Vector4 mThresholdIndicatorColor = new( 0.85f );

	//  Plugin framework and related convenience functions below.
	public void Initialize( IDalamudPluginInterface pluginInterface )
	{
		mPluginInterface = pluginInterface;
	}

	public void Save()
	{
		mPluginInterface.SavePluginConfig( this );
	}

	public List<DiffGaugeConfig> DiffGaugeConfigs { get; protected set; } = new();

	[NonSerialized]
	protected IDalamudPluginInterface mPluginInterface = null!;

	public int Version { get; set; } = 0;
}
