﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Design;
using ElectronicObserver.Resource;

namespace ElectronicObserver.Window.Control {

	/// <summary>
	/// 画像付きの Label です。
	/// </summary>
	public partial class ImageLabel : UserControl {

		private Size _imageSize = new Size( 16, 16 );
		[Browsable( true ), DefaultValue( typeof( Size ), "16, 16" ), Category( "Appearance" )]
		[Description( "イメージのサイズを指定します。" )]
		public Size ImageSize {
			get {
				if ( Image != null )
					return Image.Size;
				else
					return _imageSize;
			}
			set {
				_imageSize = value;
				AdjustSize();
				Invalidate();
			}
		}

		private int _imageMargin = 3;
		[Browsable( true ), DefaultValue( 3 ), Category( "Appearance" )]
		[Description( "イメージとラベルの間のスペースを指定します。" )]
		public int ImageMargin {
			get { return _imageMargin; }
			set {
				_imageMargin = value;
				AdjustSize();
				Invalidate();
			}
		}

		private bool _autoWrap = false;
		[Browsable( true ), DefaultValue( false ), Category( "Behavior" )]
		[Description( "幅を超えるテキストを折り返すかを指定します。" )]
		public bool AutoWrap {
			get { return _autoWrap; }
			set {
				_autoWrap = value;
				_measureTextCache = null;
				Invalidate();
			}
		}

        private bool _autoEllipsis = false;
		[DefaultValue( false )]
		public bool AutoEllipsis {
			get { return _autoEllipsis; }
			set {
				_measureTextCache = null;
				_autoEllipsis = value;
			}
		}

        private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;
        [DefaultValue( ContentAlignment.MiddleLeft )]
		public ContentAlignment TextAlign {
			get { return _textAlign; }
			set {
				_measureTextCache = null;
				_textAlign = value;
			}
		}

        private ContentAlignment _imageAlign = ContentAlignment.MiddleLeft;
        [DefaultValue( ContentAlignment.MiddleLeft )]
		public ContentAlignment ImageAlign {
			get { return _imageAlign; }
			set {
				_imageAlign = value;
				AdjustSize();
			}
		}

        private bool _useMnemonic = false;
        [DefaultValue( false )]
		public bool UseMnemonic {
			get { return _useMnemonic; }
			set {
				_measureTextCache = null;
                _useMnemonic = value;
				AdjustSize();
			}
		}

        private Image _image = null;
		[DefaultValue( null )]
		public Image Image {
			get {
                _image = (ImageIndex >= 0 && ImageList != null && ImageList.Count > ImageIndex) ? ImageList[ImageIndex] : null;
				return _image;
			}
			set {
				_image = value;
				AdjustSize();
			}
		}

        [DefaultValue(0)]
        public int ImageIndex { get; set; }
        [DefaultValue(null)]
        public ImageCollection ImageList { get; set; }

        [DefaultValue( true )]
		public new bool AutoSize {
			get { return base.AutoSize; }
			set {
				bool checkon = !base.AutoSize && value;
				base.AutoSize = value;
				if ( checkon )
					AdjustSize();
			}
		}

		[DefaultValue( typeof( Padding ), "3, 3, 3, 3" )]
		public new Padding Margin {
			get { return base.Margin; }
			set { base.Margin = value; }
		}


		private Size? _measureTextCache = null;
		private Size MeasureTextCache {
			get {
				return _measureTextCache ?? (Size)( _measureTextCache = TextRenderer.MeasureText( Text, Font, new Size( int.MaxValue, int.MaxValue ), GetTextFormat() ) );
			}
		}

		private Size? _preferredSizeCache = null;
		private Size PreferredSizeCache {
			get {
				return _preferredSizeCache ?? (Size)( _preferredSizeCache = GetPreferredSize( Size ) );
			}
		}


		public ImageLabel()
			: base() {
			TextAlign = ContentAlignment.MiddleLeft;
			ImageAlign = ContentAlignment.MiddleLeft;
			UseMnemonic = false;
			AutoSize = true;
			Margin = new Padding( 3 );
		}


		protected override void OnTextChanged( EventArgs e ) {
			_measureTextCache = null;
			AdjustSize();
			base.OnTextChanged( e );
		}

		protected override void OnFontChanged( EventArgs e ) {
			_measureTextCache = null;
			AdjustSize();
			base.OnFontChanged( e );
		}



		public void AdjustSize() {
			if ( AutoSize ) {
				_preferredSizeCache = null;
				Size = PreferredSizeCache;
			}
		}

		public Size GetPreferredSize() {

			if ( _preferredSizeCache != null )
				return (Size)PreferredSizeCache;

			// size - clientsize は border の調整用
			Size ret = new Size( Padding.Horizontal, Padding.Vertical ) + ( Size - ClientSize );

			Size sz_text = MeasureTextCache;

			if ( !string.IsNullOrEmpty( Text ) )
				sz_text.Width -= (int)( Font.Size / 2 );

			switch ( ImageAlign ) {
				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
				case ContentAlignment.TopRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.BottomRight:
					ret.Width += ImageSize.Width + ImageMargin + sz_text.Width;
					ret.Height += Math.Max( ImageSize.Height, sz_text.Height );
					break;

				case ContentAlignment.TopCenter:
				case ContentAlignment.BottomCenter:
					ret.Width += Math.Max( ImageSize.Width, sz_text.Width );
					ret.Height += ImageSize.Height + ImageMargin + sz_text.Height;
					break;

				case ContentAlignment.MiddleCenter:
					ret.Width += Math.Max( ImageSize.Width, sz_text.Width );
					ret.Height += Math.Max( ImageSize.Height, sz_text.Height );
					break;

			}

			return ret;
		}

		public override Size GetPreferredSize( Size proposedSize ) {
			var size = GetPreferredSize();
			if ( !MaximumSize.IsEmpty ) {
				size.Width = Math.Min( MaximumSize.Width, size.Width );
				size.Height = Math.Min( MaximumSize.Height, size.Height );
			}
			if ( !MinimumSize.IsEmpty ) {
				size.Width = Math.Max( MinimumSize.Width, size.Width );
				size.Height = Math.Max( MinimumSize.Height, size.Height );
			}
			return size;
		}


		private TextFormatFlags GetTextFormat() {

			TextFormatFlags textformat = TextFormatFlags.NoPadding;

			if ( AutoWrap )
				textformat |= TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak;
			if ( AutoEllipsis )
				textformat |= TextFormatFlags.EndEllipsis;
			if ( !UseMnemonic )
				textformat |= TextFormatFlags.NoPrefix;

			switch ( TextAlign ) {
				case ContentAlignment.TopLeft:
					textformat |= TextFormatFlags.Top | TextFormatFlags.Left; break;
				case ContentAlignment.TopCenter:
					textformat |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter; break;
				case ContentAlignment.TopRight:
					textformat |= TextFormatFlags.Top | TextFormatFlags.Right; break;
				case ContentAlignment.MiddleLeft:
					textformat |= TextFormatFlags.VerticalCenter | TextFormatFlags.Left; break;
				case ContentAlignment.MiddleCenter:
					textformat |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter; break;
				case ContentAlignment.MiddleRight:
					textformat |= TextFormatFlags.VerticalCenter | TextFormatFlags.Right; break;
				case ContentAlignment.BottomLeft:
					textformat |= TextFormatFlags.Bottom | TextFormatFlags.Left; break;
				case ContentAlignment.BottomCenter:
					textformat |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter; break;
				case ContentAlignment.BottomRight:
					textformat |= TextFormatFlags.Bottom | TextFormatFlags.Right; break;
			}

			return textformat;
		}


		private Rectangle GetTextArea( Rectangle area ) {

			switch ( ImageAlign ) {
				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					area.X += ImageSize.Width + ImageMargin;
					area.Width -= ImageSize.Width + ImageMargin;
					break;

				case ContentAlignment.TopRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.BottomRight:
					area.Width -= ImageSize.Width + ImageMargin;
					break;

				case ContentAlignment.TopCenter:
					area.Y += ImageSize.Height + ImageMargin;
					area.Height -= ImageSize.Height + ImageMargin;
					break;
				case ContentAlignment.BottomCenter:
					area.Height -= ImageSize.Height + ImageMargin;
					break;

				case ContentAlignment.MiddleCenter:
					break;
			}
			return area;
		}


		protected override void OnPaint( PaintEventArgs e ) {

			Rectangle basearea = new Rectangle( Padding.Left, Padding.Top, ClientSize.Width - Padding.Horizontal, ClientSize.Height - Padding.Vertical );
			//e.Graphics.DrawRectangle( Pens.Magenta, basearea.X, basearea.Y, basearea.Width - 1, basearea.Height - 1 );

			Rectangle imagearea = new Rectangle( basearea.X, basearea.Y, ImageSize.Width, ImageSize.Height );

			switch ( ImageAlign ) {
				case ContentAlignment.TopLeft:
					break;
				case ContentAlignment.MiddleLeft:
					imagearea.Y += ( basearea.Height - imagearea.Height ) / 2;
					break;
				case ContentAlignment.BottomLeft:
					imagearea.Y = basearea.Bottom - imagearea.Height;
					break;

				case ContentAlignment.TopCenter:
					imagearea.X += ( basearea.Width - imagearea.Width ) / 2;
					break;
				case ContentAlignment.MiddleCenter:
					imagearea.X += ( basearea.Width - imagearea.Width ) / 2;
					imagearea.Y += ( basearea.Height - imagearea.Height ) / 2;
					break;
				case ContentAlignment.BottomCenter:
					imagearea.X += ( basearea.Width - imagearea.Width ) / 2;
					imagearea.Y = basearea.Bottom - imagearea.Height;
					break;

				case ContentAlignment.TopRight:
					imagearea.X = basearea.Right - imagearea.Width;
					break;
				case ContentAlignment.MiddleRight:
					imagearea.X = basearea.Right - imagearea.Width;
					imagearea.Y += ( basearea.Height - imagearea.Height ) / 2;
					break;
				case ContentAlignment.BottomRight:
					imagearea.X = basearea.Right - imagearea.Width;
					imagearea.Y = basearea.Bottom - imagearea.Height;
					break;
			}


			if ( Image != null ) {
				if ( Enabled )
					e.Graphics.DrawImage( Image, imagearea );
				else
					ControlPaint.DrawImageDisabled( e.Graphics, Image, imagearea.X, imagearea.Y, BackColor );

				//e.Graphics.DrawRectangle( Pens.Orange, imagearea.X, imagearea.Y, imagearea.Width - 1, imagearea.Height - 1 );
			}


			Color textcolor;
			if ( Enabled ) {
				textcolor = ForeColor;
			} else {
				if ( BackColor.GetBrightness() < SystemColors.Control.GetBrightness() )
					textcolor = ControlPaint.Dark( BackColor );
				else
					textcolor = SystemColors.ControlDark;
			}

			TextRenderer.DrawText( e.Graphics, Text, Font, GetTextArea( basearea ), textcolor, GetTextFormat() );

			//base.OnPaint( e );
		}

	}
}
