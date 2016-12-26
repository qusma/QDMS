// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using QDMSClient;

namespace QDMSServer
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// If the request was successful, returns false.
        /// Otherwise displays dialog with errors, then returns true.
        /// </summary>
        public static async Task<bool> DisplayErrors(this ApiResponse response, object context, IDialogCoordinator dialogCoordinator)
        {
            if (response.WasSuccessful) return false;
            await dialogCoordinator.ShowMessageAsync(context, "Error", string.Join("\n", response.Errors)).ConfigureAwait(true);
            return true;
        }

        /// <summary>
        /// If all requests were successful, returns false.
        /// Otherwise displays dialog with errors, then returns true.
        /// </summary>
        public static async Task<bool> DisplayErrors(this IEnumerable<ApiResponse> responses, object context, IDialogCoordinator dialogCoordinator)
        {
            foreach (var response in responses)
            {
                if (await response.DisplayErrors(context, dialogCoordinator)) return true;
            }

            return false;
        }
    }
}
