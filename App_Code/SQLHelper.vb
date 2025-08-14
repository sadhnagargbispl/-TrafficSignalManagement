

' The SqlHelper class is intended to encapsulate high performance, scalable best practices for 
' common uses of SqlClient.

' ===============================================================================
' Release history
' VERSION	DESCRIPTION
'   2.0	Added support for FillDataset, UpdateDataset and "Param" helper methods
'
' ===============================================================================

Imports System.Data
Imports System.Data.SqlClient
Imports System.Xml

Public NotInheritable Class SqlHelper

#Region "private utility methods & constructors"

    ' Since this class provides only static methods, make the default constructor private to prevent 
    ' instances from being created with "new SqlHelper()".
    Private Sub New()
    End Sub ' New

    ' This method is used to attach array of SqlParameters to a SqlCommand.
    ' This method will assign a value of DbNull to any parameter with a direction of
    ' InputOutput and a value of null.  
    ' This behavior will prevent default values from being used, but
    ' this will be the less common case than an intended pure output parameter (derived as InputOutput)
    ' where the user provided no input value.
    ' Parameters:
    ' -command - The command to which the parameters will be added
    ' -commandParameters - an array of SqlParameters to be added to command
    Private Shared Sub AttachParameters(ByVal command As SqlCommand, ByVal commandParameters() As SqlParameter)
        Try
            If (command Is Nothing) Then Throw New ArgumentNullException("command")
            If (Not commandParameters Is Nothing) Then
                Dim p As SqlParameter
                For Each p In commandParameters
                    If (Not p Is Nothing) Then
                        ' Check for derived output value with no value assigned
                        If (p.Direction = ParameterDirection.InputOutput OrElse p.Direction = ParameterDirection.Input) AndAlso p.Value Is Nothing Then
                            p.Value = DBNull.Value
                        End If
                        command.Parameters.Add(p)
                    End If
                Next p
            End If
        Catch ex As Exception

        End Try

    End Sub ' AttachParameters

    ' This method assigns dataRow column values to an array of SqlParameters.
    ' Parameters:
    ' -commandParameters: Array of SqlParameters to be assigned values
    ' -dataRow: the dataRow used to hold the stored procedure' s parameter values
    Private Overloads Shared Sub AssignParameterValues(ByVal commandParameters() As SqlParameter, ByVal dataRow As DataRow)
        Try
            If commandParameters Is Nothing OrElse dataRow Is Nothing Then
                ' Do nothing if we get no data    
                Exit Sub
            End If

            ' Set the parameters values
            Dim commandParameter As SqlParameter
            Dim i As Integer
            For Each commandParameter In commandParameters
                ' Check the parameter name
                If (commandParameter.ParameterName Is Nothing OrElse commandParameter.ParameterName.Length <= 1) Then
                    Throw New Exception(String.Format("Please provide a valid parameter name on the parameter #{0}, the ParameterName property has the following value: ' {1}' .", i, commandParameter.ParameterName))
                End If
                If dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) <> -1 Then
                    commandParameter.Value = dataRow(commandParameter.ParameterName.Substring(1))
                End If
                i = i + 1
            Next
        Catch ex As Exception

        End Try

    End Sub

    ' This method assigns an array of values to an array of SqlParameters.
    ' Parameters:
    ' -commandParameters - array of SqlParameters to be assigned values
    ' -array of objects holding the values to be assigned
    Private Overloads Shared Sub AssignParameterValues(ByVal commandParameters() As SqlParameter, ByVal parameterValues() As Object)
        Try
            Dim i As Integer
            Dim j As Integer

            If (commandParameters Is Nothing) AndAlso (parameterValues Is Nothing) Then
                ' Do nothing if we get no data
                Return
            End If

            ' We must have the same number of values as we pave parameters to put them in
            If commandParameters.Length <> parameterValues.Length Then
                Throw New ArgumentException("Parameter count does not match Parameter Value count.")
            End If

            ' Value array
            j = commandParameters.Length - 1
            For i = 0 To j
                ' If the current array value derives from IDbDataParameter, then assign its Value property
                If TypeOf parameterValues(i) Is IDbDataParameter Then
                    Dim paramInstance As IDbDataParameter = CType(parameterValues(i), IDbDataParameter)
                    If (paramInstance.Value Is Nothing) Then
                        commandParameters(i).Value = DBNull.Value
                    Else
                        commandParameters(i).Value = paramInstance.Value
                    End If
                ElseIf (parameterValues(i) Is Nothing) Then
                    commandParameters(i).Value = DBNull.Value
                Else
                    commandParameters(i).Value = parameterValues(i)
                End If
            Next
        Catch ex As Exception

        End Try
        
    End Sub ' AssignParameterValues

    ' This method opens (if necessary) and assigns a connection, transaction, command type and parameters 
    ' to the provided command.
    ' Parameters:
    ' -command - the SqlCommand to be prepared
    ' -connection - a valid SqlConnection, on which to execute this command
    ' -transaction - a valid SqlTransaction, or ' null' 
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' -commandParameters - an array of SqlParameters to be associated with the command or ' null' if no parameters are required
    Private Shared Sub PrepareCommand(ByVal command As SqlCommand, _
                                      ByVal connection As SqlConnection, _
                                      ByVal transaction As SqlTransaction, _
                                      ByVal commandType As CommandType, _
                                      ByVal commandText As String, _
                                      ByVal commandParameters() As SqlParameter)


        Try
            If (command Is Nothing) Then Throw New ArgumentNullException("command")
            If (commandText Is Nothing OrElse commandText.Length = 0) Then Throw New ArgumentNullException("commandText")

            ' If the provided connection is not open, we will open it
            ' Associate the connection with the command
            command.Connection = connection

            ' Set the command text (stored procedure name or SQL statement)
            command.CommandText = commandText

            ' If we were provided a transaction, assign it.
            If Not (transaction Is Nothing) Then
                If transaction.Connection Is Nothing Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
                command.Transaction = transaction
            End If

            ' Set the command type
            command.CommandType = commandType

            ' Attach the command parameters if they are provided
            If Not (commandParameters Is Nothing) Then
                AttachParameters(command, commandParameters)
            End If
        Catch ex As Exception

        End Try

       
    End Sub ' PrepareCommand

#End Region

#Region "ExecuteNonQuery"

    ' Execute a SqlCommand (that returns no resultset and takes no parameters) against the database specified in 
    ' the connection string. 
    ' e.g.:  
    '  Dim result As Integer =  ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders")
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' Returns: An int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Dim i As Integer = 0
        Try
            i = ExecuteNonQuery(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return i
    End Function ' ExecuteNonQuery

    ' Execute a SqlCommand (that returns no resultset) against the database specified in the connection string 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim result As Integer = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' -commandParameters - an array of SqlParamters used to execute the command
    ' Returns: An int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, _
                                                     ByVal commandType As CommandType, _
                                                     ByVal commandText As String, _
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        ' Create & open a SqlConnection, and dispose of it after we are done
        Dim connection As SqlConnection
        Dim i As Integer = 0
        Try
            connection = New SqlConnection(connectionString)
            connection.Open()

            ' Call the overload that takes a connection in place of the connection string
            i = ExecuteNonQuery(connection, commandType, commandText, commandParameters)
            connection.Close()
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
        Return i

    End Function ' ExecuteNonQuery

    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in 
    ' the connection string using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    '  Dim result As Integer = ExecuteNonQuery(connString, "PublishOrders", 24, 36)
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -spName - the name of the stored procedure
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure
    ' Returns: An int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String, _
                                                     ByVal spName As String, _
                                                     ByVal ParamArray parameterValues() As Object) As Integer

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()

        ' If we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)

            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            ' Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            ' Call the overload that takes an array of SqlParameters
            Return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            ' Otherwise we can just call the SP without params
        Else
            Return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function ' ExecuteNonQuery

    ' Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    ' Dim result As Integer = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders")
    ' Parameters:
    ' -connection - a valid SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, _
                                                     ByVal commandType As CommandType, _
                                                     ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Return ExecuteNonQuery(connection, commandType, commandText, CType(Nothing, SqlParameter()))

    End Function ' ExecuteNonQuery

    ' Execute a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    '  Dim result As Integer = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An int representing the number of rows affected by the command 
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, _
                                                     ByVal commandType As CommandType, _
                                                     ByVal commandText As String, _
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer
        Dim retval As Integer = 0
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")

            ' Create a command and prepare it for execution
            Dim cmd As New SqlCommand
            PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)
            ' Finally, execute the command
            retval = cmd.ExecuteNonQuery()
            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
        Catch ex As Exception

        End Try
        Return retval
    End Function ' ExecuteNonQuery

    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    '  Dim result As integer = ExecuteNonQuery(conn, "PublishOrders", 24, 36)
    ' Parameters:
    ' -connection - a valid SqlConnection
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An int representing the number of rows affected by the command 
    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection, _
                                                     ByVal spName As String, _
                                                     ByVal ParamArray parameterValues() As Object) As Integer

        Dim i As Integer = 0
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                i = ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                i = ExecuteNonQuery(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return i
        

    End Function ' ExecuteNonQuery

    ' Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlTransaction.
    ' e.g.:  
    '  Dim result As Integer = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders")
    ' Parameters:
    ' -transaction - a valid SqlTransaction associated with the connection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An int representing the number of rows affected by the command 
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, _
                                                     ByVal commandType As CommandType, _
                                                     ByVal commandText As String) As Integer
        ' Pass through the call providing null for the set of SqlParameters
        Dim i As Integer = 0
        Try
            i = ExecuteNonQuery(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return i
    End Function ' ExecuteNonQuery

    ' Execute a SqlCommand (that returns no resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    ' Dim result As Integer = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An int representing the number of rows affected by the command 
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, _
                                                     ByVal commandType As CommandType, _
                                                     ByVal commandText As String, _
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer
        Dim retval As Integer
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")

            ' Create a command and prepare it for execution
            Dim cmd As New SqlCommand

            Dim mustCloseConnection As Boolean = False

            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

            ' Finally, execute the command
            retval = cmd.ExecuteNonQuery()

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
        Catch ex As Exception

        End Try


        Return retval
    End Function ' ExecuteNonQuery

    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlTransaction 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim result As Integer = SqlHelper.ExecuteNonQuery(trans, "PublishOrders", 24, 36)
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An int representing the number of rows affected by the command 
    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction, _
                                                     ByVal spName As String, _
                                                     ByVal ParamArray parameterValues() As Object) As Integer
        Dim i As Integer = 0
        Try

            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                i = ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                i = ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName)
            End If

        Catch ex As Exception

        End Try

        Return i

        
    End Function ' ExecuteNonQuery

#End Region

#Region "ExecuteDataset"

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in 
    ' the connection string. 
    ' e.g.:  
    ' Dim ds As DataSet = SqlHelper.ExecuteDataset("", commandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String) As DataSet
        ' Pass through the call providing null for the set of SqlParameters

        Dim tempDataSet As New DataSet()
        Try
            tempDataSet = ExecuteDataset(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return tempDataSet
    End Function ' ExecuteDataset

    ' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' -commandParameters - an array of SqlParamters used to execute the command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String, _
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet
        Dim tempDs As New DataSet()
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")

        ' Create & open a SqlConnection, and dispose of it after we are done
        Dim connection As SqlConnection
        connection = New SqlConnection(connectionString)
        Try
            connection.Open()
            ' Call the overload that takes a connection in place of the connection string
            tempDs = ExecuteDataset(connection, commandType, commandText, commandParameters)
            connection.Close()
        Finally
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
        Return tempDs
    End Function ' ExecuteDataset



    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ' the connection string using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim ds As Dataset= ExecuteDataset(connString, "GetOrders", 24, 36)
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -spName - the name of the stored procedure
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String, _
                                                    ByVal spName As String, _
                                                    ByVal ParamArray parameterValues() As Object) As DataSet

        Dim tempDs As New DataSet()
        Try
            If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                tempDs = ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                tempDs = ExecuteDataset(connectionString, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return tempDs


    End Function ' ExecuteDataset

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -connection - a valid SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String) As DataSet

        ' Pass through the call providing null for the set of SqlParameters
        Dim TempDs As New DataSet()
        Try
            TempDs = ExecuteDataset(connection, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return TempDs
    End Function ' ExecuteDataset

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection - a valid SqlConnection
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' -commandParameters - an array of SqlParamters used to execute the command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String, _
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet

        Dim ds As New DataSet
        ' Create a command and prepare it for execution

        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")

            Dim cmd As New SqlCommand

            Dim dataAdatpter As SqlDataAdapter

            PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)


            ' Create the DataAdapter & DataSet
            dataAdatpter = New SqlDataAdapter(cmd)

            ' Fill the DataSet using default values for DataTable names, etc
            dataAdatpter.Fill(ds)

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()

            ' Return the dataset
        Catch ex As Exception

        End Try



        Return ds
    End Function ' ExecuteDataset

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(conn, "GetOrders", 24, 36)
    ' Parameters:
    ' -connection - a valid SqlConnection
    ' -spName - the name of the stored procedure
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection, _
                                                    ByVal spName As String, _
                                                    ByVal ParamArray parameterValues() As Object) As DataSet

        Dim ds As New DataSet

        Try

            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                ds = ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                ds = ExecuteDataset(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try

        Return Ds


    End Function ' ExecuteDataset

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders")
    ' Parameters
    ' -transaction - a valid SqlTransaction
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String) As DataSet
        ' Pass through the call providing null for the set of SqlParameters
        Dim ds As New DataSet
        Try
            ds = ExecuteDataset(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return ds
    End Function ' ExecuteDataset

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters
    ' -transaction - a valid SqlTransaction 
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command
    ' -commandParameters - an array of SqlParamters used to execute the command
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String, _
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet

 

        
        Dim ds As New DataSet
        Try

            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")

            ' Create a command and prepare it for execution
            Dim cmd As New SqlCommand

            Dim dataAdatpter As SqlDataAdapter
            Dim mustCloseConnection As Boolean = False

            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

            ' Create the DataAdapter & DataSet
            dataAdatpter = New SqlDataAdapter(cmd)

            ' Fill the DataSet using default values for DataTable names, etc
            dataAdatpter.Fill(ds)

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
            dataAdatpter.Dispose()
        Finally

        End Try

        ' Return the dataset
        Return ds

    End Function ' ExecuteDataset

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
    ' SqlTransaction using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim ds As Dataset = ExecuteDataset(trans, "GetOrders", 24, 36)
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -spName - the name of the stored procedure
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction, _
                                                    ByVal spName As String, _
                                                    ByVal ParamArray parameterValues() As Object) As DataSet
        Dim ds As New DataSet
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                ds = ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                ds = ExecuteDataset(transaction, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return ds
    End Function ' ExecuteDataset

#End Region

#Region "ExecuteReader"
    ' this enum is used to indicate whether the connection was provided by the caller, or created by SqlHelper, so that
    ' we can set the appropriate CommandBehavior when calling ExecuteReader()
    Private Enum SqlConnectionOwnership
        ' Connection is owned and managed by SqlHelper
        Internal
        ' Connection is owned and managed by the caller
        [External]
    End Enum ' SqlConnectionOwnership

    ' Create and prepare a SqlCommand, and call ExecuteReader with the appropriate CommandBehavior.
    ' If we created and opened the connection, we want the connection to be closed when the DataReader is closed.
    ' If the caller provided the connection, we want to leave it to them to manage.
    ' Parameters:
    ' -connection - a valid SqlConnection, on which to execute this command 
    ' -transaction - a valid SqlTransaction, or ' null' 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParameters to be associated with the command or ' null' if no parameters are required 
    ' -connectionOwnership - indicates whether the connection parameter was provided by the caller, or created by SqlHelper 
    ' Returns: SqlDataReader containing the results of the command 
    Private Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, _
                                                    ByVal transaction As SqlTransaction, _
                                                    ByVal commandType As CommandType, _
                                                    ByVal commandText As String, _
                                                    ByVal commandParameters() As SqlParameter, _
                                                    ByVal connectionOwnership As SqlConnectionOwnership) As SqlDataReader
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")

        Dim mustCloseConnection As Boolean = False
        ' Create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim dataReader As SqlDataReader
        Try
            ' Create a reader


            PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters)

            ' Call ExecuteReader with the appropriate CommandBehavior
            If connectionOwnership = SqlConnectionOwnership.External Then
                dataReader = cmd.ExecuteReader()
            Else
                dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection)
            End If

            ' Detach the SqlParameters from the command object, so they can be used again
            Dim canClear As Boolean = True
            Dim commandParameter As SqlParameter
            For Each commandParameter In cmd.Parameters
                If commandParameter.Direction <> ParameterDirection.Input Then
                    canClear = False
                End If
            Next

            If (canClear) Then cmd.Parameters.Clear()
        Catch ex As Exception
            Throw
        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in 
    ' the connection string. 
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As SqlDataReader
        ' Pass through the call providing null for the set of SqlParameters
        Dim dataReader As SqlDataReader
        Try
            dataReader = ExecuteReader(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")

        ' Create & open a SqlConnection
        Dim connection As SqlConnection
        Dim dataReader As SqlDataReader
        Try
            connection = New SqlConnection(connectionString)
            connection.Open()
            ' Call the private overload that takes an internally owned connection in place of the connection string
            dataReader = ExecuteReader(connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, SqlConnectionOwnership.Internal)
        Catch
            ' If we fail to return the SqlDatReader, we need to close the connection ourselves
          
        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ' the connection string using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(connString, "GetOrders", 24, 36)
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader

        Dim dataReader As SqlDataReader
        Try
            If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                dataReader = ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters)
                ' Otherwise we can just call the SP without params
            Else
                dataReader = ExecuteReader(connectionString, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return dataReader

    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As SqlDataReader
        Dim dataReader As SqlDataReader
        Try
            dataReader = ExecuteReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return dataReader

    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        ' Pass through the call to private overload using a null transaction value
        Dim dataReader As SqlDataReader
        Try
            dataReader = ExecuteReader(connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, SqlConnectionOwnership.External)
        Catch ex As Exception

        End Try

        Return dataReader
    End Function ' ExecuteReader

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(conn, "GetOrders", 24, 36)
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader
        Dim dataReader As SqlDataReader
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()
            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                AssignParameterValues(commandParameters, parameterValues)

                dataReader = ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                dataReader = ExecuteReader(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -transaction - a valid SqlTransaction  
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As SqlDataReader
        ' Pass through the call providing null for the set of SqlParameters
        Dim dataReader As SqlDataReader
        Try
            dataReader = ExecuteReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -commandType - the CommandType (stored procedure, text, etc.)
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: A SqlDataReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        Dim dataReader As SqlDataReader
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            ' Pass through to private overload, indicating that the connection is owned by the caller
            dataReader = ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SqlConnectionOwnership.External)
        Catch ex As Exception

        End Try
        Return dataReader
    End Function ' ExecuteReader

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim dr As SqlDataReader = ExecuteReader(trans, "GetOrders", 24, 36)
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure
    ' Returns: A SqlDataReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader
        Dim dataReader As SqlDataReader
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            Dim commandParameters As SqlParameter()

            ' If we receive parameter values, we need to figure out where they go
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                AssignParameterValues(commandParameters, parameterValues)

                dataReader = ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                dataReader = ExecuteReader(transaction, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return dataReader
    End Function ' ExecuteReader

#End Region

#Region "ExecuteScalar"

    ' Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the database specified in 
    ' the connection string. 
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount"))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command
    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Dim objTemp As Object
        Try
            objTemp = ExecuteScalar(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try
        Return objTemp
    End Function ' ExecuteScalar

    ' Execute a SqlCommand (that returns a 1x1 resultset) against the database specified in the connection string 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim orderCount As Integer = Cint(ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24)))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object

        Dim objTemp As Object

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        ' Create & open a SqlConnection, and dispose of it after we are done.
        Dim connection As SqlConnection
        Try
            connection = New SqlConnection(connectionString)
            connection.Open()

            ' Call the overload that takes a connection in place of the connection string
            objTemp = ExecuteScalar(connection, commandType, commandText, commandParameters)
            connection.Close()
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try

        Return objTemp

    End Function ' ExecuteScalar

    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in 
    ' the connection string using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(connString, "GetOrderCount", 24, 36))
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As Object

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()
        Dim objTemp As Object
        Try
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                objTemp = ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters)
                ' Otherwise we can just call the SP without params
            Else
                objTemp = ExecuteScalar(connectionString, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return objTemp
        ' If we receive parameter values, we need to figure out where they go

    End Function ' ExecuteScalar

    ' Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount"))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Try
            Return ExecuteScalar(connection, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try

    End Function ' ExecuteScalar

    ' Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24)))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object

        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")

        ' Create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Object
        Try
            PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

            ' Execute the command & return the results
            retval = cmd.ExecuteScalar()

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()

        Catch ex As Exception

        End Try

        Return retval

    End Function ' ExecuteScalar

    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(conn, "GetOrderCount", 24, 36))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As Object



        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()
        Dim objTemp As Object


        Try
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                objTemp = ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else ' Otherwise we can just call the SP without params
                objTemp = ExecuteScalar(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return objTemp
        ' If we receive parameter values, we need to figure out where they go


    End Function ' ExecuteScalar

    ' Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlTransaction.
    ' e.g.:  
    ' Dim orderCount As Integer  = CInt(ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount"))
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String) As Object
        ' Pass through the call providing null for the set of SqlParameters
        Try
            Return ExecuteScalar(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try

    End Function ' ExecuteScalar

    ' Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24)))
    ' Parameters:
    ' -transaction - a valid SqlTransaction  
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, _
                                                   ByVal commandType As CommandType, _
                                                   ByVal commandText As String, _
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")

        ' Create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Object

        Try
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

            ' Execute the command & return the results
            retval = cmd.ExecuteScalar()

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
        Catch ex As Exception

        End Try

        

        Return retval
    End Function ' ExecuteScalar

    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim orderCount As Integer = CInt(ExecuteScalar(trans, "GetOrderCount", 24, 36))
    ' Parameters:
    ' -transaction - a valid SqlTransaction 
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An object containing the value in the 1x1 resultset generated by the command 
    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction, _
                                                   ByVal spName As String, _
                                                   ByVal ParamArray parameterValues() As Object) As Object
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()
        Dim ObjTemp As Object
        ' If we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

            ' Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            ' Call the overload that takes an array of SqlParameters
            ObjTemp = ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else ' Otherwise we can just call the SP without params
            ObjTemp = ExecuteScalar(transaction, CommandType.StoredProcedure, spName)
        End If
        Return ObjTemp

    End Function ' ExecuteScalar

#End Region

#Region "ExecuteXmlReader"

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    ' Returns: An XmlReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, _
                                                      ByVal commandType As CommandType, _
                                                      ByVal commandText As String) As XmlReader
        ' Pass through the call providing null for the set of SqlParameters
        Try
            Return ExecuteXmlReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try

    End Function ' ExecuteXmlReader

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An XmlReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, _
                                                      ByVal commandType As CommandType, _
                                                      ByVal commandText As String, _
                                                      ByVal ParamArray commandParameters() As SqlParameter) As XmlReader
        ' Pass through the call using a null transaction value
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        ' Create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As XmlReader
        Try


            PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

            ' Create the DataAdapter & DataSet
            retval = cmd.ExecuteXmlReader()

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()

        Catch ex As Exception

        End Try
        Return retval
    End Function ' ExecuteXmlReader

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(conn, "GetOrders", 24, 36)
    ' Parameters:
    ' -connection - a valid SqlConnection 
    ' -spName - the name of the stored procedure using "FOR XML AUTO" 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: An XmlReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection, _
                                                      ByVal spName As String, _
                                                      ByVal ParamArray parameterValues() As Object) As XmlReader



        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()
        Dim retval As XmlReader

        ' If we receive parameter values, we need to figure out where they go
        Try
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                retval = ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters)
                ' Otherwise we can just call the SP without params
            Else
                retval = ExecuteXmlReader(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return retval
    End Function ' ExecuteXmlReader


    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders")
    ' Parameters:
    ' -transaction - a valid SqlTransaction
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    ' Returns: An XmlReader containing the resultset generated by the command 
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, _
                                                      ByVal commandType As CommandType, _
                                                      ByVal commandText As String) As XmlReader
        ' Pass through the call providing null for the set of SqlParameters
        Try
            Return ExecuteXmlReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
        Catch ex As Exception

        End Try

    End Function ' ExecuteXmlReader

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -transaction - a valid SqlTransaction
    ' -commandType - the CommandType (stored procedure, text, etc.) 
    ' -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    ' -commandParameters - an array of SqlParamters used to execute the command 
    ' Returns: An XmlReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, _
                                                      ByVal commandType As CommandType, _
                                                      ByVal commandText As String, _
                                                      ByVal ParamArray commandParameters() As SqlParameter) As XmlReader
        ' Create a command and prepare it for execution
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")

        Dim cmd As New SqlCommand

        Dim retval As XmlReader
        Try
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

            ' Create the DataAdapter & DataSet
            retval = cmd.ExecuteXmlReader()

            ' Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear()
        Catch ex As Exception

        End Try



        Return retval

    End Function ' ExecuteXmlReader

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ' using the provided parameter values.  This method will discover the parameters for the 
    ' stored procedure, and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' Dim r As XmlReader = ExecuteXmlReader(trans, "GetOrders", 24, 36)
    ' Parameters:
    ' -transaction - a valid SqlTransaction
    ' -spName - the name of the stored procedure 
    ' -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    ' Returns: A dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction, _
                                                      ByVal spName As String, _
                                                      ByVal ParamArray parameterValues() As Object) As XmlReader
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        Dim commandParameters As SqlParameter()
        Dim retval As XmlReader
        ' If we receive parameter values, we need to figure out where they go
        Try
            If Not (parameterValues Is Nothing) AndAlso parameterValues.Length > 0 Then
                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                retval = ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
                ' Otherwise we can just call the SP without params
            Else
                retval = ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return retval
    End Function ' ExecuteXmlReader

#End Region

#Region "FillDataset"
    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in 
    ' the connection string. 
    ' e.g.:  
    '   FillDataset (connString, CommandType.StoredProcedure, "GetOrders", ds, new String() {"orders"})
    ' Parameters:    
    ' -connectionString: A valid connection string for a SqlConnection
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '               by a user defined name (probably the actual table name)
    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String)

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")

        ' Create & open a SqlConnection, and dispose of it after we are done
        Dim connection As SqlConnection

        Try
            connection = New SqlConnection(connectionString)

            connection.Open()

            ' Call the overload that takes a connection in place of the connection string
            FillDataset(connection, commandType, commandText, dataSet, tableNames)
            connection.Close()
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
    End Sub

    ' Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
    ' using the provided parameters.
    ' e.g.:  
    '   FillDataset (connString, CommandType.StoredProcedure, "GetOrders", ds, new String() = {"orders"}, new SqlParameter("@prodid", 24))
    ' Parameters:    
    ' -connectionString: A valid connection string for a SqlConnection
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '               by a user defined name (probably the actual table name)
    ' -commandParameters: An array of SqlParamters used to execute the command
    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal commandType As CommandType, ByVal commandText As String, ByVal dataSet As DataSet, _
        ByVal tableNames() As String, ByVal ParamArray commandParameters() As SqlParameter)

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")

        ' Create & open a SqlConnection, and dispose of it after we are done
        Dim connection As SqlConnection
        Try
            connection = New SqlConnection(connectionString)

            connection.Open()

            ' Call the overload that takes a connection in place of the connection string
            FillDataset(connection, commandType, commandText, dataSet, tableNames, commandParameters)
            connection.Close()
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
    End Sub

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ' the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    '   FillDataset (connString, CommandType.StoredProcedure, "GetOrders", ds, new String() {"orders"}, 24)
    ' Parameters:
    ' -connectionString: A valid connection string for a SqlConnection
    ' -spName: the name of the stored procedure
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '             by a user defined name (probably the actual table name)
    ' -parameterValues: An array of objects to be assigned As the input values of the stored procedure
    Public Overloads Shared Sub FillDataset(ByVal connectionString As String, ByVal spName As String, _
        ByVal dataSet As DataSet, ByVal tableNames As String(), ByVal ParamArray parameterValues() As Object)

        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")

        ' Create & open a SqlConnection, and dispose of it after we are done
        Dim connection As SqlConnection
        Try
            connection = New SqlConnection(connectionString)

            connection.Open()

            ' Call the overload that takes a connection in place of the connection string
            FillDataset(connection, spName, dataSet, tableNames, parameterValues)
            connection.Close()
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
    End Sub

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    ' e.g.:  
    '   FillDataset (conn, CommandType.StoredProcedure, "GetOrders", ds, new String() {"orders"})
    ' Parameters:
    ' -connection: A valid SqlConnection
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    ' by a user defined name (probably the actual table name)
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, _
        ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames As String())
        Try
            FillDataset(connection, commandType, commandText, dataSet, tableNames, Nothing)
        Catch ex As Exception

        End Try


    End Sub

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameters.
    ' e.g.:  
    '   FillDataset (conn, CommandType.StoredProcedure, "GetOrders", ds, new String() {"orders"}, new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection: A valid SqlConnection
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    ' by a user defined name (probably the actual table name)
    ' -commandParameters: An array of SqlParamters used to execute the command
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal commandType As CommandType, _
    ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames As String(), _
        ByVal ParamArray commandParameters() As SqlParameter)
        Try
            FillDataset(connection, Nothing, commandType, commandText, dataSet, tableNames, commandParameters)
        Catch ex As Exception

        End Try


    End Sub

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the provided parameter values.  This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    ' FillDataset (conn, "GetOrders", ds, new string() {"orders"}, 24, 36)
    ' Parameters:
    ' -connection: A valid SqlConnection
    ' -spName: the name of the stored procedure
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '             by a user defined name (probably the actual table name)
    ' -parameterValues: An array of objects to be assigned as the input values of the stored procedure
    Public Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataSet As DataSet, _
        ByVal tableNames() As String, ByVal ParamArray parameterValues() As Object)
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
            If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            ' If we receive parameter values, we need to figure out where they go
            If Not parameterValues Is Nothing AndAlso parameterValues.Length > 0 Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters)
            Else ' Otherwise we can just call the SP without params
                FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames)
            End If
        Catch ex As Exception

        End Try


    End Sub

    ' Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction. 
    ' e.g.:  
    '   FillDataset (trans, CommandType.StoredProcedure, "GetOrders", ds, new string() {"orders"})
    ' Parameters:
    ' -transaction: A valid SqlTransaction
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '             by a user defined name (probably the actual table name)
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, _
        ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String)
        Try
            FillDataset(transaction, commandType, commandText, dataSet, tableNames, Nothing)
        Catch ex As Exception

        End Try

    End Sub

    ' Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    ' using the provided parameters.
    ' e.g.:  
    '   FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string() {"orders"}, new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -transaction: A valid SqlTransaction
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    ' by a user defined name (probably the actual table name)
    ' -commandParameters: An array of SqlParamters used to execute the command
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal commandType As CommandType, _
        ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, _
        ByVal ParamArray commandParameters() As SqlParameter)
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            FillDataset(transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters)
        Catch ex As Exception

        End Try


    End Sub

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified 
    ' SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' This method provides no access to output parameters or the stored procedure' s return value parameter.
    ' e.g.:  
    '   FillDataset(trans, "GetOrders", ds, new String(){"orders"}, 24, 36)
    ' Parameters:
    ' -transaction: A valid SqlTransaction
    ' -spName: the name of the stored procedure
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '             by a user defined name (probably the actual table name)
    ' -parameterValues: An array of objects to be assigned as the input values of the stored procedure
    Public Overloads Shared Sub FillDataset(ByVal transaction As SqlTransaction, ByVal spName As String, _
        ByVal dataSet As DataSet, ByVal tableNames() As String, ByVal ParamArray parameterValues() As Object)
        Try
            If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
            If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
            If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            ' If we receive parameter values, we need to figure out where they go
            If Not parameterValues Is Nothing AndAlso parameterValues.Length > 0 Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues)

                ' Call the overload that takes an array of SqlParameters
                FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters)
            Else ' Otherwise we can just call the SP without params
                FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames)
            End If
        Catch ex As Exception

        End Try

    End Sub

    ' Private helper method that execute a SqlCommand (that returns a resultset) against the specified SqlTransaction and SqlConnection
    ' using the provided parameters.
    ' e.g.:  
    '   FillDataset(conn, trans, CommandType.StoredProcedure, "GetOrders", ds, new String() {"orders"}, new SqlParameter("@prodid", 24))
    ' Parameters:
    ' -connection: A valid SqlConnection
    ' -transaction: A valid SqlTransaction
    ' -commandType: the CommandType (stored procedure, text, etc.)
    ' -commandText: the stored procedure name or T-SQL command
    ' -dataSet: A dataset wich will contain the resultset generated by the command
    ' -tableNames: this array will be used to create table mappings allowing the DataTables to be referenced
    '             by a user defined name (probably the actual table name)
    ' -commandParameters: An array of SqlParamters used to execute the command
    Private Overloads Shared Sub FillDataset(ByVal connection As SqlConnection, ByVal transaction As SqlTransaction, ByVal commandType As CommandType, _
        ByVal commandText As String, ByVal dataSet As DataSet, ByVal tableNames() As String, _
        ByVal ParamArray commandParameters() As SqlParameter)

        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")

        ' Create a command and prepare it for execution
        Dim command As New SqlCommand



        Try
            ' Add the table mappings specified by the user


            PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters)

            ' Create the DataAdapter & DataSet
            Dim dataAdapter As SqlDataAdapter = New SqlDataAdapter(command)

            If Not tableNames Is Nothing AndAlso tableNames.Length > 0 Then

                Dim tableName As String = "Table"
                Dim index As Integer

                For index = 0 To tableNames.Length - 1
                    If (tableNames(index) Is Nothing OrElse tableNames(index).Length = 0) Then Throw New ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", "tableNames")
                    dataAdapter.TableMappings.Add(tableName, tableNames(index))
                    tableName = tableName & (index + 1).ToString()
                Next
            End If

            ' Fill the DataSet using default values for DataTable names, etc
            dataAdapter.Fill(dataSet)

            ' Detach the SqlParameters from the command object, so they can be used again
            command.Parameters.Clear()
        Catch ex As Exception

        End Try

        
    End Sub
#End Region

#Region "UpdateDataset"
    ' Executes the respective command for each inserted, updated, or deleted row in the DataSet.
    ' e.g.:  
    '   UpdateDataset(conn, insertCommand, deleteCommand, updateCommand, dataSet, "Order")
    ' Parameters:
    ' -insertCommand: A valid transact-SQL statement or stored procedure to insert new records into the data source
    ' -deleteCommand: A valid transact-SQL statement or stored procedure to delete records from the data source
    ' -updateCommand: A valid transact-SQL statement or stored procedure used to update records in the data source
    ' -dataSet: the DataSet used to update the data source
    ' -tableName: the DataTable used to update the data source
    Public Overloads Shared Sub UpdateDataset(ByVal insertCommand As SqlCommand, ByVal deleteCommand As SqlCommand, ByVal updateCommand As SqlCommand, ByVal dataSet As DataSet, ByVal tableName As String)

        If (insertCommand Is Nothing) Then Throw New ArgumentNullException("insertCommand")
        If (deleteCommand Is Nothing) Then Throw New ArgumentNullException("deleteCommand")
        If (updateCommand Is Nothing) Then Throw New ArgumentNullException("updateCommand")
        If (dataSet Is Nothing) Then Throw New ArgumentNullException("dataSet")
        If (tableName Is Nothing OrElse tableName.Length = 0) Then Throw New ArgumentNullException("tableName")

        ' Create a SqlDataAdapter, and dispose of it after we are done
        Dim dataAdapter As New SqlDataAdapter
        Try
            ' Set the data adapter commands
            dataAdapter.UpdateCommand = updateCommand
            dataAdapter.InsertCommand = insertCommand
            dataAdapter.DeleteCommand = deleteCommand

            ' Update the dataset changes in the data source
            dataAdapter.Update(dataSet, tableName)

            ' Commit all the changes made to the DataSet
            dataSet.AcceptChanges()
        Catch ex As Exception

        End Try
    End Sub
#End Region

#Region "CreateCommand"
    ' Simplify the creation of a Sql command object by allowing
    ' a stored procedure and optional parameters to be provided
    ' e.g.:  
    ' Dim command As SqlCommand = CreateCommand(conn, "AddCustomer", "CustomerID", "CustomerName")
    ' Parameters:
    ' -connection: A valid SqlConnection object
    ' -spName: the name of the stored procedure
    ' -sourceColumns: An array of string to be assigned as the source columns of the stored procedure parameters
    ' Returns:
    ' a valid SqlCommand object
    Public Overloads Shared Function CreateCommand(ByVal connection As SqlConnection, ByVal spName As String, ByVal ParamArray sourceColumns() As String) As SqlCommand

        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        ' Create a SqlCommand
        Dim cmd As New SqlCommand(spName, connection)
        cmd.CommandType = CommandType.StoredProcedure

        ' If we receive parameter values, we need to figure out where they go
        If Not sourceColumns Is Nothing AndAlso sourceColumns.Length > 0 Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

            ' Assign the provided source columns to these parameters based on parameter order
            Dim index As Integer
            For index = 0 To sourceColumns.Length - 1
                commandParameters(index).SourceColumn = sourceColumns(index)
            Next

            ' Attach the discovered parameters to the SqlCommand object
            AttachParameters(cmd, commandParameters)
        End If

        CreateCommand = cmd
    End Function
#End Region

#Region "ExecuteNonQueryTypedParams"
    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in 
    ' the connection string using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -connectionString: A valid connection string for a SqlConnection
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values
    ' Returns:
    ' an int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As Integer
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        Dim i As Integer = 0
        ' If the row has values, the store procedure parameters must be initialized
        Try
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                i = SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            Else
                i = SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return i
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection 
    ' using the dataRow column values as the stored procedure' s parameters values.  
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -connection:a valid SqlConnection object
    ' -spName: the name of the stored procedure
    ' -dataRow:The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' an int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As Integer
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        Dim i As Integer = 0
        ' If the row has values, the store procedure parameters must be initialized
        Try
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                i = SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else
                i = SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return i
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified
    ' SqlTransaction using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -transaction:a valid SqlTransaction object
    ' -spName:the name of the stored procedure
    ' -dataRow:The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' an int representing the number of rows affected by the command
    Public Overloads Shared Function ExecuteNonQueryTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As Integer

        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        Dim i As Integer = 0
        Try
            ' If the row has values, the store procedure parameters must be initialized
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                i = SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters)
            Else
                i = SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return i
    End Function
#End Region

#Region "ExecuteDatasetTypedParams"
    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ' the connection string using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -connectionString: A valid connection string for a SqlConnection
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As DataSet
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        Dim TempDs As New DataSet()
        Try
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                TempDs = SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            Else

                TempDs = SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return TempDs
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the dataRow column values as the store procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -connection: A valid SqlConnection object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As DataSet

        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        Dim TempDs As New DataSet()

        ' If the row has values, the store procedure parameters must be initialized
        Try
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                TempDs = SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters)
            Else

                TempDs = SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        Return TempDs
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    ' Parameters:
    ' -transaction: A valid SqlTransaction object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a dataset containing the resultset generated by the command
    Public Overloads Shared Function ExecuteDatasetTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As DataSet
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        Dim TempDs As New DataSet()
        Try
            If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

                ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

                ' Set the parameters values
                AssignParameterValues(commandParameters, dataRow)

                TempDs = SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters)
            Else

                TempDs = SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName)
            End If
        Catch ex As Exception

        End Try
        ' If the row has values, the store procedure parameters must be initialized
        Return TempDs
    End Function
#End Region

#Region "ExecuteReaderTypedParams"
    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
    ' the connection string using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -connectionString: A valid connection string for a SqlConnection
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a SqlDataReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -connection: A valid SqlConnection object
    ' -spName: The name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a SqlDataReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName)
        End If
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -transaction: A valid SqlTransaction object
    ' -spName" The name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' a SqlDataReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteReaderTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As SqlDataReader
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteReaderTypedParams = SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function
#End Region

#Region "ExecuteScalarTypedParams"
    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in 
    ' the connection string using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -connectionString: A valid connection string for a SqlConnection
    ' -spName: The name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns:
    ' An object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal connectionString As String, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -connection: A valid SqlConnection object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns: 
    ' an object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName)
        End If
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -transaction: A valid SqlTransaction object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns: 
    ' an object containing the value in the 1x1 resultset generated by the command</returns>
    Public Overloads Shared Function ExecuteScalarTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As Object
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteScalarTypedParams = SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function
#End Region

#Region "ExecuteXmlReaderTypedParams"
    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -connection: A valid SqlConnection object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns: 
    ' an XmlReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteXmlReaderTypedParams(ByVal connection As SqlConnection, ByVal spName As String, ByVal dataRow As DataRow) As XmlReader
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        ' If the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteXmlReaderTypedParams = SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteXmlReaderTypedParams = SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName)
        End If
    End Function

    ' Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    ' using the dataRow column values as the stored procedure' s parameters values.
    ' This method will query the database to discover the parameters for the 
    ' stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    ' Parameters:
    ' -transaction: A valid SqlTransaction object
    ' -spName: the name of the stored procedure
    ' -dataRow: The dataRow used to hold the stored procedure' s parameter values.
    ' Returns: 
    ' an XmlReader containing the resultset generated by the command
    Public Overloads Shared Function ExecuteXmlReaderTypedParams(ByVal transaction As SqlTransaction, ByVal spName As String, ByVal dataRow As DataRow) As XmlReader
        If (transaction Is Nothing) Then Throw New ArgumentNullException("transaction")
        If Not (transaction Is Nothing) AndAlso (transaction.Connection Is Nothing) Then Throw New ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction")
        If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
        ' if the row has values, the store procedure parameters must be initialized
        If (Not dataRow Is Nothing AndAlso dataRow.ItemArray.Length > 0) Then

            ' Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            Dim commandParameters() As SqlParameter = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName)

            ' Set the parameters values
            AssignParameterValues(commandParameters, dataRow)

            ExecuteXmlReaderTypedParams = SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
        Else
            ExecuteXmlReaderTypedParams = SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function
#End Region
End Class ' SqlHelper

' SqlHelperParameterCache provides functions to leverage a static cache of procedure parameters, and the
' ability to discover parameters for stored procedures at run-time.
Public NotInheritable Class SqlHelperParameterCache

#Region "private methods, variables, and constructors"


    ' Since this class provides only static methods, make the default constructor private to prevent 
    ' instances from being created with "new SqlHelperParameterCache()".
    Private Sub New()
    End Sub ' New 

    Private Shared paramCache As Hashtable = Hashtable.Synchronized(New Hashtable)

    ' resolve at run time the appropriate set of SqlParameters for a stored procedure
    ' Parameters:
    ' - connectionString - a valid connection string for a SqlConnection
    ' - spName - the name of the stored procedure
    ' - includeReturnValueParameter - whether or not to include their return value parameter>
    ' Returns: SqlParameter()
    Private Shared Function DiscoverSpParameterSet(ByVal connection As SqlConnection, _
                                                       ByVal spName As String, _
                                                       ByVal includeReturnValueParameter As Boolean, _
                                                       ByVal ParamArray parameterValues() As Object) As SqlParameter()
        Dim discoveredParameters() As SqlParameter
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")
            Dim cmd As New SqlCommand(spName, connection)
            cmd.CommandType = CommandType.StoredProcedure

            connection.Open()
            SqlCommandBuilder.DeriveParameters(cmd)
            connection.Close()
            If Not includeReturnValueParameter Then
                cmd.Parameters.RemoveAt(0)
            End If

            Dim discoveredParameter As SqlParameter
            discoveredParameters = New SqlParameter(cmd.Parameters.Count - 1) {}
            cmd.Parameters.CopyTo(discoveredParameters, 0)

            ' Init the parameters with a DBNull value

            For Each discoveredParameter In discoveredParameters
                discoveredParameter.Value = DBNull.Value
            Next

        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try

        Return discoveredParameters

    End Function ' DiscoverSpParameterSet

    ' Deep copy of cached SqlParameter array
    Private Shared Function CloneParameters(ByVal originalParameters() As SqlParameter) As SqlParameter()

        Dim i As Integer
        Dim j As Integer = originalParameters.Length - 1
        Dim clonedParameters(j) As SqlParameter

        For i = 0 To j
            clonedParameters(i) = CType(CType(originalParameters(i), ICloneable).Clone, SqlParameter)
        Next

        Return clonedParameters
    End Function ' CloneParameters

#End Region

#Region "caching functions"

    ' add parameter array to the cache
    ' Parameters
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandText - the stored procedure name or T-SQL command 
    ' -commandParameters - an array of SqlParamters to be cached 
    Public Shared Sub CacheParameterSet(ByVal connectionString As String, _
                                        ByVal commandText As String, _
                                        ByVal ParamArray commandParameters() As SqlParameter)
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (commandText Is Nothing OrElse commandText.Length = 0) Then Throw New ArgumentNullException("commandText")

        Dim hashKey As String = connectionString + ":" + commandText

        paramCache(hashKey) = commandParameters
    End Sub ' CacheParameterSet

    ' retrieve a parameter array from the cache
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -commandText - the stored procedure name or T-SQL command 
    ' Returns: An array of SqlParamters 
    Public Shared Function GetCachedParameterSet(ByVal connectionString As String, ByVal commandText As String) As SqlParameter()
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        If (commandText Is Nothing OrElse commandText.Length = 0) Then Throw New ArgumentNullException("commandText")

        Dim hashKey As String = connectionString + ":" + commandText
        Dim cachedParameters As SqlParameter() = CType(paramCache(hashKey), SqlParameter())

        If cachedParameters Is Nothing Then
            Return Nothing
        Else
            Return CloneParameters(cachedParameters)
        End If
    End Function ' GetCachedParameterSet

#End Region

#Region "Parameter Discovery Functions"
    ' Retrieves the set of SqlParameters appropriate for the stored procedure.
    ' This method will query the database for this information, and then store it in a cache for future requests.
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection 
    ' -spName - the name of the stored procedure 
    ' Returns: An array of SqlParameters
    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String, ByVal spName As String) As SqlParameter()
        Return GetSpParameterSet(connectionString, spName, False)
    End Function ' GetSpParameterSet 

    ' Retrieves the set of SqlParameters appropriate for the stored procedure.
    ' This method will query the database for this information, and then store it in a cache for future requests.
    ' Parameters:
    ' -connectionString - a valid connection string for a SqlConnection
    ' -spName - the name of the stored procedure 
    ' -includeReturnValueParameter - a bool value indicating whether the return value parameter should be included in the results 
    ' Returns: An array of SqlParameters 
    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String, _
                                                       ByVal spName As String, _
                                                       ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If (connectionString Is Nothing OrElse connectionString.Length = 0) Then Throw New ArgumentNullException("connectionString")
        Dim connection As SqlConnection
        Dim temSqParam As SqlParameter()
        Try
            connection = New SqlConnection(connectionString)
            temSqParam = GetSpParameterSetInternal(connection, spName, includeReturnValueParameter)
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
        Return temSqParam
    End Function ' GetSpParameterSet

    ' Retrieves the set of SqlParameters appropriate for the stored procedure.
    ' This method will query the database for this information, and then store it in a cache for future requests.
    ' Parameters:
    ' -connection - a valid SqlConnection object
    ' -spName - the name of the stored procedure 
    ' -includeReturnValueParameter - a bool value indicating whether the return value parameter should be included in the results 
    ' Returns: An array of SqlParameters 
    Public Overloads Shared Function GetSpParameterSet(ByVal connection As SqlConnection, _
                                                       ByVal spName As String) As SqlParameter()
        Dim temSqParam As SqlParameter()
        Try
            temSqParam = GetSpParameterSet(connection, spName, False)
        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
        Return temSqParam
    End Function ' GetSpParameterSet

    ' Retrieves the set of SqlParameters appropriate for the stored procedure.
    ' This method will query the database for this information, and then store it in a cache for future requests.
    ' Parameters:
    ' -connection - a valid SqlConnection object
    ' -spName - the name of the stored procedure 
    ' -includeReturnValueParameter - a bool value indicating whether the return value parameter should be included in the results 
    ' Returns: An array of SqlParameters 
    Public Overloads Shared Function GetSpParameterSet(ByVal connection As SqlConnection, _
                                                       ByVal spName As String, _
                                                       ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        If (connection Is Nothing) Then Throw New ArgumentNullException("connection")
        Dim clonedConnection As SqlConnection
        Try
            clonedConnection = CType((CType(connection, ICloneable).Clone), SqlConnection)
            GetSpParameterSet = GetSpParameterSetInternal(clonedConnection, spName, includeReturnValueParameter)
        Catch ex As Exception
            If Not clonedConnection Is Nothing Then
                If clonedConnection.State = ConnectionState.Open Then
                    clonedConnection.Close()
                End If
            End If
        End Try
    End Function ' GetSpParameterSet

    ' Retrieves the set of SqlParameters appropriate for the stored procedure.
    ' This method will query the database for this information, and then store it in a cache for future requests.
    ' Parameters:
    ' -connection - a valid SqlConnection object
    ' -spName - the name of the stored procedure 
    ' -includeReturnValueParameter - a bool value indicating whether the return value parameter should be included in the results 
    ' Returns: An array of SqlParameters 
    Private Overloads Shared Function GetSpParameterSetInternal(ByVal connection As SqlConnection, _
                                                    ByVal spName As String, _
                                                    ByVal includeReturnValueParameter As Boolean) As SqlParameter()
        Dim cachedParameters() As SqlParameter
        Try
            If (connection Is Nothing) Then Throw New ArgumentNullException("connection")


            Dim hashKey As String

            If (spName Is Nothing OrElse spName.Length = 0) Then Throw New ArgumentNullException("spName")

            hashKey = connection.ConnectionString + ":" + spName + IIf(includeReturnValueParameter = True, ":include ReturnValue Parameter", "").ToString

            cachedParameters = CType(paramCache(hashKey), SqlParameter())

            If (cachedParameters Is Nothing) Then
                Dim spParameters() As SqlParameter = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter)
                paramCache(hashKey) = spParameters
                cachedParameters = spParameters

            End If


        Catch ex As Exception
            If Not connection Is Nothing Then
                If connection.State = ConnectionState.Open Then
                    connection.Close()
                End If
            End If
        End Try
        Return CloneParameters(cachedParameters)

    End Function ' GetSpParameterSet
#End Region

End Class ' SqlHelperParameterCache 