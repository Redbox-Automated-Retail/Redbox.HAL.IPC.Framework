using Redbox.HAL.Component.Model;
using Redbox.HAL.Core;
using System;
using System.Collections.Generic;

namespace Redbox.IPC.Framework
{
    public class IPCProtocol : IIpcProtocol
    {
        private string PipeName;
        private readonly string RawUri;

        public static IPCProtocol Parse(string protocolURI)
        {
            IPCProtocol ipcProtocol = new IPCProtocol(protocolURI)
            {
                Host = string.Empty,
                Port = string.Empty,
                IsSecure = false,
                Channel = ChannelType.Unknown
            };
            IPCProtocol.ProtocolTokenizer protocolTokenizer = new IPCProtocol.ProtocolTokenizer(protocolURI);
            try
            {
                protocolTokenizer.Tokenize();
            }
            catch (Exception ex)
            {
                throw new UriFormatException(ex.Message);
            }
            if (protocolTokenizer.Errors.ContainsError())
            {
                foreach (Error error in (List<Error>)protocolTokenizer.Errors)
                    LogHelper.Instance.Log(error.Description, LogEntryType.Error);
                throw new UriFormatException("URI is malformed: see log for details.");
            }
            ipcProtocol.Host = protocolTokenizer.Host;
            ipcProtocol.Port = protocolTokenizer.Port;
            ipcProtocol.IsSecure = protocolTokenizer.IsSecure;
            ipcProtocol.Channel = protocolTokenizer.Channel;
            ipcProtocol.Scheme = protocolTokenizer.Scheme;
            if (string.IsNullOrEmpty(ipcProtocol.Host) || string.IsNullOrEmpty(ipcProtocol.Port))
                throw new UriFormatException("Host or port isn't set.");
            if (ipcProtocol.Channel == ChannelType.Unknown)
                throw new UriFormatException("The channel type is unknown; please correct your URI.");
            if (ipcProtocol.Channel == ChannelType.Socket && !int.TryParse(ipcProtocol.Port, out int _))
                throw new UriFormatException("Protocol is set up for sockets, but port isn't a valid address.");
            return ipcProtocol;
        }

        public string GetPipeName()
        {
            if (this.PipeName == null)
                this.PipeName = string.Format("{0}{1}", (object)this.Host, (object)this.Port);
            return this.PipeName;
        }

        public bool IsSecure { get; private set; }

        public ChannelType Channel { get; private set; }

        public string Host { get; private set; }

        public string Port { get; private set; }

        public string Scheme { get; private set; }

        public override string ToString() => this.RawUri;

        private IPCProtocol(string rawUri) => this.RawUri = rawUri;

        private enum ProtocolParserState
        {
            Start = 1,
            Scheme = 2,
            Host = 3,
            Port = 4,
            Channel = 5,
            Whitespace = 6,
            Error = 7,
        }

        private class ProtocolTokenizer : Tokenizer<IPCProtocol.ProtocolParserState>
        {
            private int m_slashesSeen;
            private int m_semiColonsSeen;

            public ProtocolTokenizer(string statement) : base(0, statement)
            {
            }

            protected override void OnReset()
            {
                this.CurrentState = IPCProtocol.ProtocolParserState.Start;
            }

            internal ChannelType Channel { get; private set; }

            internal bool IsSecure { get; private set; }

            internal string Host { get; private set; }

            internal string Port { get; private set; }

            internal string Scheme { get; private set; }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Error)]
            internal StateResult ProcessErrorState()
            {
                this.ResetAccumulator();
                this.Errors.Add(Error.NewError("T006", this.FormatError("An invalid token was detected."), "Correct your protocol syntax and resubmit."));
                return StateResult.Terminal;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Start)]
            internal StateResult ProcessStartState()
            {
                if (char.IsWhiteSpace(this.GetCurrentToken()))
                    return StateResult.Continue;
                if (char.IsLetter(this.GetCurrentToken()))
                {
                    this.CurrentState = IPCProtocol.ProtocolParserState.Scheme;
                    return StateResult.Restart;
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Host)]
            internal StateResult ProcessHostState()
            {
                char currentToken = this.GetCurrentToken();
                if (char.IsWhiteSpace(currentToken))
                    return StateResult.Continue;
                if (char.IsLetterOrDigit(currentToken) || currentToken == '.')
                {
                    this.AppendToAccumulator();
                    return StateResult.Continue;
                }
                if (':' == currentToken)
                {
                    this.Host = this.Accumulator.ToString();
                    this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                    this.CurrentState = IPCProtocol.ProtocolParserState.Whitespace;
                    return StateResult.Restart;
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Scheme)]
            internal StateResult ProcessProtocolState()
            {
                char currentToken = this.GetCurrentToken();
                if (char.IsLetter(currentToken))
                {
                    if (this.Accumulator.Length == 0 && ('s' == currentToken || 'S' == currentToken))
                        this.IsSecure = true;
                    this.AppendToAccumulator();
                    return StateResult.Continue;
                }
                if ('-' == currentToken)
                {
                    this.MoveToNextToken();
                    this.Scheme = this.Accumulator.ToString();
                    if (this.ValidateProtocol())
                    {
                        this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                        this.CurrentState = IPCProtocol.ProtocolParserState.Channel;
                        return StateResult.Restart;
                    }
                    this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                    return StateResult.Restart;
                }
                if (':' == currentToken || char.IsWhiteSpace(currentToken))
                {
                    this.Scheme = this.Accumulator.ToString();
                    if (this.ValidateProtocol())
                    {
                        this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                        this.CurrentState = IPCProtocol.ProtocolParserState.Whitespace;
                        return StateResult.Restart;
                    }
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Port)]
            internal StateResult ProcessPortState()
            {
                char currentToken = this.GetCurrentToken();
                if (char.IsWhiteSpace(currentToken))
                {
                    if (this.Accumulator.Length <= 0)
                        return StateResult.Continue;
                    this.Port = this.Accumulator.ToString();
                    this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                    return StateResult.Terminal;
                }
                if (char.IsLetterOrDigit(currentToken))
                {
                    this.AppendToAccumulator();
                    if (this.PeekNextToken().HasValue)
                        return StateResult.Continue;
                    this.Port = this.Accumulator.ToString();
                    this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                    this.MoveToNextToken();
                    return StateResult.Terminal;
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Channel)]
            internal StateResult ProcessChannelType()
            {
                char currentToken = this.GetCurrentToken();
                if (char.IsWhiteSpace(currentToken) || ':' == currentToken)
                {
                    this.DecodeChannel();
                    this.AddTokenAndReset(Redbox.HAL.Core.TokenType.StringLiteral, false);
                    this.CurrentState = IPCProtocol.ProtocolParserState.Whitespace;
                    return StateResult.Restart;
                }
                if (char.IsLetter(currentToken))
                {
                    this.AppendToAccumulator();
                    return StateResult.Continue;
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            [StateHandler(State = IPCProtocol.ProtocolParserState.Whitespace)]
            internal StateResult ProcessWhitespaceState()
            {
                char currentToken = this.GetCurrentToken();
                if (char.IsWhiteSpace(currentToken))
                    return StateResult.Continue;
                if (currentToken == ':')
                {
                    ++this.m_semiColonsSeen;
                    return StateResult.Continue;
                }
                if (currentToken == '/')
                {
                    ++this.m_slashesSeen;
                    return StateResult.Continue;
                }
                if (char.IsLetterOrDigit(currentToken))
                {
                    if (this.m_slashesSeen == 0 && this.m_semiColonsSeen == 0)
                    {
                        this.CurrentState = IPCProtocol.ProtocolParserState.Scheme;
                        return StateResult.Restart;
                    }
                    if (this.m_semiColonsSeen == 1 && this.m_slashesSeen == 2)
                    {
                        this.CurrentState = IPCProtocol.ProtocolParserState.Host;
                        return StateResult.Restart;
                    }
                    if (this.m_semiColonsSeen == 2 && this.m_slashesSeen == 2)
                    {
                        this.CurrentState = IPCProtocol.ProtocolParserState.Port;
                        return StateResult.Restart;
                    }
                }
                this.CurrentState = IPCProtocol.ProtocolParserState.Error;
                return StateResult.Restart;
            }

            private bool ValidateProtocol()
            {
                return this.Accumulator.ToString().Equals("rcp", StringComparison.CurrentCultureIgnoreCase) || this.Accumulator.ToString().Equals("srcp", StringComparison.CurrentCultureIgnoreCase);
            }

            private bool ChannelIsValid(char protocol)
            {
                if ('p' != protocol && 'P' != protocol)
                    return false;
                this.Channel = ChannelType.NamedPipe;
                return true;
            }

            private bool DecodeChannel()
            {
                if (this.Accumulator.ToString().Equals("p", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.Channel = ChannelType.NamedPipe;
                    return true;
                }
                this.Channel = ChannelType.Unknown;
                return false;
            }
        }
    }
}
