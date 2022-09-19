using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildManager
{
	public class PlatformIcon : BindableElement, INotifyValueChanged<BuildTarget>
	{
		public new class UxmlTraits : BindableElement.UxmlTraits { }
		public new class UxmlFactory : UxmlFactory<PlatformIcon, UxmlTraits> { }
		
		public BuildTarget m_Value;
		
		public BuildTarget value
		{
			get => m_Value;
			
			set
			{
				if (value == this.value)
					return;

				var previous = this.value;
				SetValueWithoutNotify(value);

				using (var evt = ChangeEvent<BuildTarget>.GetPooled(previous, value))
				{
					evt.target = this;
					SendEvent(evt);
				}
			}
		}
		
		public PlatformIcon()
		{
			style.flexDirection = FlexDirection.Row;
		}
		

		public void SetValueWithoutNotify(BuildTarget newValue)
		{
			Clear();
			for(BuildTarget t = (BuildTarget)1;;t = (BuildTarget)((int)t << 1))
			{
				if (!Enum.IsDefined(typeof(BuildTarget), t))
				{
					break;
				}
				
				if (!newValue.HasFlag(t))
				{
					continue;
				}
				
				Add(MakeIcon(t));
			}
		}

		private void Test(ChangeEvent<BuildTarget> evt)
		{
			SetValueWithoutNotify(evt.newValue);
		}

		private VisualElement MakeIcon(BuildTarget target)
		{
			VisualElement icon = new VisualElement
			{
				style =
				{
					width = 16,
					height = 16,
					marginRight = 2,
					backgroundImage = GetIcon(target)
				}
			};
			return icon;
		}

		#region Platform Icon
        
		private static StyleBackground IconStandalone => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.Standalone"));
		private static StyleBackground IconLinux => new StyleBackground(EditorGUIUtility.FindTexture("BuildSettings.EmbeddedLinux"));
		private static StyleBackground IconAndroid => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.Android"));
		private static StyleBackground IconIos => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.iPhone"));
		private static StyleBackground IconServer => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.Server"));
		private static StyleBackground IconXbox => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.GameCoreXboxOne"));
		private static StyleBackground IconUWP => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.Metro"));
		private static StyleBackground IconPS4 => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.PS4"));
		private static StyleBackground IconPS5 => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.PS5"));
		private static StyleBackground IconOther => new StyleBackground(EditorGUIUtility.FindTexture("d_BuildSettings.SelectedIcon"));
        
		#endregion
		
		private static StyleBackground GetIcon(BuildTarget target)
		{
			switch (target)
			{
				case BuildTarget.Linux:
					return IconLinux;
				case BuildTarget.Mac:
					return IconStandalone;
				case BuildTarget.Android:
					return IconAndroid;
				case BuildTarget.IOS:
				case BuildTarget.AppleTV:
					return IconIos;
				case BuildTarget.Windows:
				case BuildTarget.UWP:
					return IconUWP;
				case BuildTarget.WebGL:
				default:
					return IconOther;
			}
		}
	}
}