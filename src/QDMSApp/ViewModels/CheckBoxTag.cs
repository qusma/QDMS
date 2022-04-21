// -----------------------------------------------------------------------
// <copyright file="CheckBoxTag.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;

namespace QDMSApp
{
    public class CheckBoxTag : CheckBoxItem<Tag>
    {
        public CheckBoxTag(Tag item, bool isChecked = false) : base(item, isChecked)
        {
        }
    }
}
