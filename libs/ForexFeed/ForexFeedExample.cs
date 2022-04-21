/*
 * ForexFeed.Net Data API
 *
 * Copyright 2009 ForexFeed.Net <copyright@forexfeed.net>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *      This product includes software developed by ForexFeed.Net.
 * 4. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using System.Collections;

// Load the ForexFeed.net API
using forexfeed.net;

class ForexFeedExample {
    
    //    ------------------------------------------
    //    EDIT THE FOLLOWING VARIABLES
    //    
    
    private static string access_key = "YOUR_ACCESS_KEY";
    private static string symbol = "EURUSD,GBPUSD,USDCHF,USDCAD,AUDUSD";
    private static int interval = 3600;
    private static int periods = 1;
    private static string price = "mid";
    
    //    END VARIABLES
    //    ------------------------------------------
    //   



    //   ------------------------------------------
    //   Main
    //   
    static void Main() {
        //  Create the ForexFeed Object
        feedapi fxfeed = new feedapi(access_key, symbol, interval, periods, price);
        //  fxfeed.setPrice("bid,ask")
        
        // Display a Conversion
        printConversion(fxfeed);

        //  Display the Quotes
        printData(fxfeed);

        //  Display the available Intervals
        printIntervals(fxfeed);

        //  Display the available Symbols
        printSymbols(fxfeed);
    }

    
    // ''  
    // ''  Get a conversion and print it to System.Console
    // ''   
    private static void printConversion(feedapi fxfeed) {

       // Hashtable conversion = fxfeed.getConversion("EUR", "USD", "1");
        Hashtable conversion = fxfeed.getConversion("USD", "EUR", "1");

        Console.WriteLine("-------- Conversion --------");
        if (fxfeed.getStatus().Equals("OK")) {
            Console.Write(conversion["convert_value"] + " ");
            Console.Write(conversion["convert_from"] + " = ");
            Console.Write(conversion["conversion_value"] + " ");
            Console.Write(conversion["convert_to"] + " ");
            Console.WriteLine("(rate: " + conversion["conversion_rate"] + ")");
            Console.WriteLine("");
        }
        else {
            Console.WriteLine(("Status: " + fxfeed.getStatus()));
            Console.WriteLine(("ErrorCode: " + fxfeed.getErrorCode()));
            Console.WriteLine(("ErrorMessage: " + fxfeed.getErrorMessage()));
        }
    }

    // ''  
    // ''  Get the data and print it to System.Console
    // ''   
    private static void printData(feedapi fxfeed) {
        //     
        //   Fetch the Data
        //      
        ArrayList quotes = fxfeed.getData();
        Console.WriteLine("-------- Quotes --------");
        if (fxfeed.getStatus().Equals("OK")) {
            Console.WriteLine(("Number of Quotes: " + fxfeed.getNumQuotes()));
            Console.WriteLine(("Copyright: " + fxfeed.getCopyright()));
            Console.WriteLine(("Website: " + fxfeed.getWebsite()));
            Console.WriteLine(("License: " + fxfeed.getLicense()));
            Console.WriteLine(("Redistribution: " + fxfeed.getRedistribution()));
            Console.WriteLine(("AccessPeriod: " + fxfeed.getAccessPeriod()));
            Console.WriteLine(("AccessPerPeriod: " + fxfeed.getAccessPerPeriod()));
            Console.WriteLine(("AccessThisPeriod: " + fxfeed.getAccessThisPeriod()));
            Console.WriteLine(("AccessRemainingThisPeriod: " + fxfeed.getAccessPeriodRemaining()));
            Console.WriteLine(("AccessPeriodBegan: " + fxfeed.getAccessPeriodBegan()));
            Console.WriteLine(("NextAccessPeriodStarts: " + fxfeed.getAccessPeriodStarts()));

            //       
            //   Get an Iterator object for the quotes ArrayList using iterator() method.
            //        
            IEnumerator itr = quotes.GetEnumerator();

            //       
            //   Iterate through the ArrayList iterator
            //        
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Iterating through Quotes...");
            Console.WriteLine("----------------------------------------");
            while (itr.MoveNext()){
                Hashtable quote = ((Hashtable)(itr.Current));
                Console.WriteLine(("Quote Symbol: " + quote["symbol"]));
                Console.WriteLine(("Title: " + quote["title"]));
                Console.WriteLine(("Time: " + quote["time"]));

                if ((fxfeed.getInterval() == 1)) {
                    if (fxfeed.getPrice().Equals("bid,ask")) {
                        Console.WriteLine(("Bid: " + quote["bid"]));
                        Console.WriteLine(("Ask: " + quote["ask"]));
                    }
                    else {
                        Console.WriteLine(("Price: " + quote["price"]));
                    }
                }
                else {
                    Console.WriteLine(("Open: " + quote["open"]));
                    Console.WriteLine(("High: " + quote["high"]));
                    Console.WriteLine(("Low: " + quote["low"]));
                    Console.WriteLine(("Close: " + quote["close"]));
                }
                Console.WriteLine("");
            }
        }
        else {
            Console.WriteLine(("Status: " + fxfeed.getStatus()));
            Console.WriteLine(("ErrorCode: " + fxfeed.getErrorCode()));
            Console.WriteLine(("ErrorMessage: " + fxfeed.getErrorMessage()));
        }
    }

    // ''  
    // ''  Print the Intervals to System.Console
    // ''   
    private static void printIntervals(feedapi fxfeed) {
        //     
        //   Fetch the Intervals
        //      
        Hashtable intervals = fxfeed.getAvailableIntervals(false);
        Console.WriteLine("-------- Intervals --------");
        if (fxfeed.getStatus().Equals("OK")) {
            //       
            //   Get a Collection of values contained in HashMap
            //        
            ICollection c = intervals.Values;

            //       
            //   Obtain an Iterator for Collection
            //        
            IEnumerator itr = c.GetEnumerator();

            //       
            //   Iterate through the HashMap values iterator
            //        
            while (itr.MoveNext()) {
                Hashtable value = ((Hashtable)(itr.Current));
                Console.WriteLine(("Interval: " + value["interval"]));
                Console.WriteLine(("Title: " + value["title"]));
                Console.WriteLine("");
            }
        }
        else {
            Console.WriteLine(("Status: " + fxfeed.getStatus()));
            Console.WriteLine(("ErrorCode: " + fxfeed.getErrorCode()));
            Console.WriteLine(("ErrorMessage: " + fxfeed.getErrorMessage()));
        }
    }

    // ''  
    // ''  Print the Symbols to System.Console
    // ''   
    private static void printSymbols(feedapi fxfeed) {
        //     
        //   Fetch the Symbols
        //      
        Hashtable symbols = fxfeed.getAvailableSymbols(false);
        Console.WriteLine("-------- Symbols --------");
        if (fxfeed.getStatus().Equals("OK")) {
            //       
            //   Get a Collection of values contained in HashMap
            //        
            ICollection c = symbols.Values;

            //       
            //   Obtain an Iterator for Collection
            //        
            IEnumerator itr = c.GetEnumerator();

            //       
            //   Iterate through the HashMap values iterator
            //        
            while (itr.MoveNext()) {
                Hashtable value = ((Hashtable)(itr.Current));
                Console.WriteLine(("Symbol: " + value["symbol"]));
                Console.WriteLine(("Title: " + value["title"]));
                Console.WriteLine(("Decimals: " + value["decimals"]));
                Console.WriteLine("");
            }
        }
        else {
            Console.WriteLine(("Status: " + fxfeed.getStatus()));
            Console.WriteLine(("ErrorCode: " + fxfeed.getErrorCode()));
            Console.WriteLine(("ErrorMessage: " + fxfeed.getErrorMessage()));
        }
    }
}