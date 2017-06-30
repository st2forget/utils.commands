﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using st2forget.console.utils;

namespace st2forget.utils.commands
{
    public abstract class Command : ICommand
    {
        public abstract string CommandName { get; }
        public abstract string Description { get; }
        
        protected IList<CommandArgument> Schemas { get; set; }
        protected IList<string> Arguments { get; set; }

        protected Command()
        {
            Schemas = new List<CommandArgument>
            {
                new CommandArgument
                {
                    Name = "help",
                    ShortName = "h",
                    Description = "Shows detail of the command."
                }
            };
            Arguments = new List<string>();
        }

        protected abstract void OnExecute();

        protected abstract ICommand Filter();

        public void Execute()
        {
            var isHelp = HasArgument("help");
            if (isHelp)
            {
                Help();
            }
            else
            {
                OnExecute();
            }
        }

        protected void AddArgument(string name, string shortName = "", string description = "", bool isReqruied = false, bool isUnary = false)
        {
            if (Schemas.Any(c => c.Name.Equals(name)))
            {
                throw new ArgumentException($"Argument {name} is already defined.");
            }
            Schemas.Add(new CommandArgument
            {
                Name = name,
                ShortName = shortName,
                Description = description,
                IsRequired = isReqruied,
                IsUninary = isUnary
            });
        }

        public virtual ICommand ReadArguments(IEnumerable<string> args)
        {
            Arguments = args.ToList();
            Filter();
            return this;
        }

        protected virtual T ReadArgument<T>(string name)
        {
            var scheme = Schemas.FirstOrDefault(s => s.Name.Equals(name) || s.ShortName.Equals(name));
            if (scheme == null)
            {
                throw new ArgumentException($"Argument {{f:Yellow}}{name}{{f:d}} is not defined.");
            }

            var regStr = $"(-{scheme.ShortName}|--{scheme.Name})[:=]+";
            var regex = new Regex(regStr);

            var result = Arguments.FirstOrDefault(a => regex.IsMatch(a));

            if (!string.IsNullOrWhiteSpace(result))
            {
                result = regex.Replace(result.Trim(), "");
                if (string.IsNullOrWhiteSpace(result))
                {
                    return default(T);
                }
                return (T) Convert.ChangeType(result, typeof(T));
            }

            if (scheme.IsRequired)
            {
                throw new ArgumentException($"Missing argument {{f:Yellow}}{name}{{f:d}}.");
            }

            return default(T);
        }

        protected virtual bool HasArgument(string argument)
        {
            var scheme = Schemas.First(s => s.Name.Equals(argument) || s.ShortName.Equals(argument));
            if (scheme == null)
            {
                return false;
            }
            return Arguments.Any(arg =>
            {
                var regex = new Regex($"(-{scheme.ShortName}|--{scheme.Name})");
                return !string.IsNullOrWhiteSpace(arg) && regex.IsMatch(arg);
            });
        }

        public virtual void Help()
        {
            var commands = string.Empty;
            foreach (var schema in Schemas)
            {
                var name = string.IsNullOrWhiteSpace(schema.ShortName)
                    ? $"-{schema.Name}"
                    : $"-{schema.ShortName}|--{schema.Name}:<{schema.Name}>";

                if (!schema.IsRequired)
                {
                    name = $"[{name}]";
                }
                commands += $"{commands} {name}";
            }
            $"{{f:Green}}Command:{{f:d}} {CommandName} {commands}".PrettyPrint(ConsoleColor.White);
            $"{{f:Green}}Description:{{f:d}} {Description}".PrettyPrint(ConsoleColor.White);
            foreach (var schema in Schemas)
            {
                var isRequired = schema.IsRequired ? "{f:Red}Required{f:d}" : "";
                $"{{f:Green}}{schema.ShortName}|{schema.Name}{{f:d}}{{t:30}}{isRequired}{{t:20}}{Description}".PrettyPrint(ConsoleColor.White);
            }
            @"
{f:Yellow}*** Notes:{f:d}
    {f:Green}[{option}]{f:d}{t:40}The option is not required.
".PrettyPrint(ConsoleColor.White);
        }
    }
}