﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Tests
{
    [TestClass]
    public class NumberPromptTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NumberPromptWithEmptyIdShouldNotFail()
        {
            var emptyId = "";
            var numberPrompt = new NumberPrompt<int>(emptyId);
        }

        [TestMethod]
        public void NumberPromptWithNullIdShouldNotFail()
        {
            var nullId = "";
            nullId = null;
            var numberPrompt = new NumberPrompt<int>(nullId);
        }

        [TestMethod]
        public async Task NumberPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState))
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var numberPrompt = new NumberPrompt<int>("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." } };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (int)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("42")
            .AssertReply("Bot received the number '42'.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task NumberPromptRetry()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);
            
            var numberPrompt = new NumberPrompt<int>("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." },
                        RetryPrompt = new Activity {  Type = ActivityTypes.Message, Text = "You must enter a number." }
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (int)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("hello")
            .AssertReply("You must enter a number.")
            .Send("64")
            .AssertReply("Bot received the number '64'.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task NumberPromptValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState))
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            var dialogs = new DialogSet(dialogState);

            PromptValidator<int> validator = (promptContext, cancellationToken) =>
            {
                var result = promptContext.Recognized.Value;
                
                if (result < 100 && result > 0)
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            };
            var numberPrompt = new NumberPrompt<int>("NumberPrompt", validator, Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." },
                        RetryPrompt = new Activity {  Type = ActivityTypes.Message, Text = "You must enter a positive number less than 100." }
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (int)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("150")
            .AssertReply("You must enter a positive number less than 100.")
            .Send("64")
            .AssertReply("Bot received the number '64'.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task NumberPromptDataValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState))
                ;//.Use(new TranscriptLoggerMiddleware(new UnitTestFileLogger()));

            var dialogs = new DialogSet(dialogState);

            var numberPrompt = new IntegerPrompt(defaultLocale: Culture.English)
            {
                MinValue = 0,
                MaxValue = 100,
                InitialPrompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." },
                RetryPrompt = new Activity { Type = ActivityTypes.Message, Text = "Let's try again. Enter a number." },
                NoMatchResponse = new Activity { Type = ActivityTypes.Message, Text = "That's not a number." },
                TooSmallResponse = new Activity { Type = ActivityTypes.Message, Text = "You must enter a positive number." },
                TooLargeResponse = new Activity { Type = ActivityTypes.Message, Text = "You must enter a less than 100." },
            };
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions
                    {
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (int)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
                .AssertReply("Enter a number.")
            .Send("xyz")
                .AssertReply("That's not a number.")
                .AssertReply("Let's try again. Enter a number.")
            .Send("150")
                .AssertReply("You must enter a less than 100.")
                .AssertReply("Let's try again. Enter a number.")
            .Send("-30")
                .AssertReply("You must enter a positive number.")
                .AssertReply("Let's try again. Enter a number.")
            .Send("64")
                .AssertReply("Bot received the number '64'.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task FloatNumberPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            var numberPrompt = new FloatPrompt("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." }
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (float)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("3.14")
            .AssertReply("Bot received the number '3.14'.")
            .StartTestAsync();
        }
        
        [TestMethod]
        public async Task LongNumberPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            var numberPrompt = new NumberPrompt<long>("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." }
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (long)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("42")
            .AssertReply("Bot received the number '42'.")
            .StartTestAsync();
        }
        
        [TestMethod]
        public async Task DoubleNumberPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            var numberPrompt = new NumberPrompt<double>("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." }
                    };
                    await dc.PromptAsync("NumberPrompt", options);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (double)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("3.14")
            .AssertReply("Bot received the number '3.14'.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task DecimalNumberPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            var numberPrompt = new NumberPrompt<decimal>("NumberPrompt", defaultLocale: Culture.English);
            dialogs.Add(numberPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions
                    {
                        Prompt = new Activity { Type = ActivityTypes.Message, Text = "Enter a number." }
                    };
                    await dc.PromptAsync("NumberPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var numberResult = (decimal)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{numberResult}'."), cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("Enter a number.")
            .Send("3.14")
            .AssertReply("Bot received the number '3.14'.")
            .StartTestAsync();
        }
    }
}
