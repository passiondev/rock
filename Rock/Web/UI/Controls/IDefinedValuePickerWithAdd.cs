
namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDefinedValuePickerWithAdd
    {
        /// <summary>
        /// Gets the selected defined values identifier.
        /// </summary>
        /// <value>
        /// The selected defined values identifier.
        /// </value>
        int[] SelectedDefinedValuesId { get; set; }

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        void LoadDefinedValues();

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        /// <param name="selectedDefinedValueIds">The selected defined value ids.</param>
        void LoadDefinedValues( int[] selectedDefinedValueIds );

    }
}
