lexer grammar CQLexer;

channels { COMMENTS_AND_FORMATTING }

// Comments
Comment
    : '//' ~[\r\n]* -> channel(COMMENTS_AND_FORMATTING)
    ;

BlockComment
    : '/*' .*? '*/' -> channel(COMMENTS_AND_FORMATTING)
    ;

// Keywords
START       : 'START';
FROM        : 'FROM';
SELECT      : 'SELECT';
EXTEND      : 'EXTEND';
USEINDEX    : 'USEINDEX';
FILTER      : 'FILTER';
RETURN      : 'RETURN';
DEFINE      : 'DEFINE';
INDEX       : 'INDEX';
FUNCTION    : 'FUNCTION';
PARAMS      : 'PARAMS';
RETURNS     : 'RETURNS';
USEFUNCTION : 'USEFUNCTION';
USEINDEXFORARRAY : 'USEINDEXFORARRAY';
CREATE      : 'CREATE';
ANCESTORS   : 'ANCESTORS';
CSHARP      : 'CSHARP';

// Types
STRING_TYPE : 'string';
STRING_ARRAY_TYPE : 'string[]';

// Operators
EQUALS      : '=';
NOT_EQUALS  : '!=';
NOT         : 'not';
AND         : 'and';
OR          : 'or';

// Punctuation
LPAREN      : '(';
RPAREN      : ')';
LBRACE      : '{';
RBRACE      : '}';
COMMA       : ',';
DOT         : '.';
COLON       : ':';
SEMICOLON   : ';';

// Literals
NULL        : 'null';

// Regular strings
STRING
    : '"' (~["\\\r\n] | EscapeSequence)* '"'
    ;

// Backtick strings for C# code
BACKTICK_STRING
    : '`' (~[`])* '`'
    ;

fragment
EscapeSequence
    : '\\' [btnfr"'\\]
    | '\\' ([0-3]? [0-7])? [0-7]
    | '\\' 'u'+ HexDigit HexDigit HexDigit HexDigit
    ;

fragment
HexDigit
    : [0-9a-fA-F]
    ;

// Numbers
NUMBER
    : [0-9]+ ('.' [0-9]+)?
    ;

// Identifiers
IDENTIFIER
    : [a-zA-Z_] [a-zA-Z0-9_]*
    ;

// Whitespace
WS
    : [ \t\r\n]+ -> skip
    ;