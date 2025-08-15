parser grammar CQParser;

options { 
    tokenVocab = CQLexer; 
}

// Root rule
root
    : statement* EOF
    ;

// Main statements
statement
    : queryStatement
    | indexDefinition
    | functionDefinition
    ;

// Query statement
queryStatement
    : startFromClause
      (selectClause | extendClause)?
      useIndexClause?
      filterClause?
      returnClause
    ;

// Index definition
indexDefinition
    : DEFINE INDEX IDENTIFIER
      startFromClause
      (selectClause | extendClause)?
      (useIndexClause | useIndexForArrayClause)*
      createClause
    ;

// Function definition
functionDefinition
    : DEFINE FUNCTION IDENTIFIER
      paramsClause
      returnsClause
    ;

// Clauses
startFromClause
    : START FROM IDENTIFIER
    ;

selectClause
    : SELECT assignmentList
    ;

extendClause
    : EXTEND assignmentList
    ;

useIndexClause
    : USEINDEX IDENTIFIER
    ;

filterClause
    : FILTER expression
    ;

returnClause
    : RETURN expressionList
    ;

useIndexForArrayClause
    : USEINDEXFORARRAY IDENTIFIER
    ;

createClause
    : CREATE assignmentList
    ;

paramsClause
    : PARAMS typeSpecifier
    ;

returnsClause
    : RETURNS typeSpecifier
    ;

typeSpecifier
    : STRING_TYPE
    | STRING_ARRAY_TYPE
    | IDENTIFIER
    ;

// Assignments and expressions
assignmentList
    : assignment (COMMA assignment)*
    ;

assignment
    : IDENTIFIER EQUALS expression
    | expression
    ;

expressionList
    : expression (COMMA expression)*
    ;

expression
    : functionCall
    | fieldAccess
    | objectLiteral
    | literal
    | IDENTIFIER
    | LPAREN expression RPAREN
    | expression binaryOperator expression
    ;

binaryOperator
    : NOT_EQUALS
    | EQUALS
    | AND
    | OR
    ;

functionCall
    : functionName LPAREN argumentList? RPAREN
    ;

functionName
    : ANCESTORS
    | CSHARP
    | IDENTIFIER
    ;

argumentList
    : expression (COMMA expression)*
    ;

fieldAccess
    : IDENTIFIER (DOT IDENTIFIER)+
    ;

objectLiteral
    : LBRACE objectMemberList? RBRACE
    ;

objectMemberList
    : objectMember (COMMA objectMember)*
    ;

objectMember
    : IDENTIFIER COLON expression
    ;

literal
    : STRING
    | BACKTICK_STRING
    | NUMBER
    | NULL
    ;