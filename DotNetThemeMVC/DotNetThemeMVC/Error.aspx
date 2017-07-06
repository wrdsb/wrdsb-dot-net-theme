<% Response.StatusCode = 404 %>
<!DOCTYPE html>
<html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>

    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">

    <title>The robot didn't like that.</title>

    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">

    <!-- Bootstrap -->
    <link href="<%=(String.Format("{0}{1}{2}", "https://s3.amazonaws.com/wrdsb-ui-assets/",
        ConfigurationManager.AppSettings("awsVersion").ToString(),
        "/css/bootstrap.css"))%>" rel="stylesheet" media="all" />
    <link href="<%=(String.Format("{0}{1}{2}", "https://s3.amazonaws.com/wrdsb-ui-assets/",
        ConfigurationManager.AppSettings("awsVersion").ToString(),
        "/css/bootstrap-theme.css"))%>" rel="stylesheet" />
    <link href="<%=(String.Format("{0}{1}{2}", "https://s3.amazonaws.com/wrdsb-ui-assets/",
        ConfigurationManager.AppSettings("awsVersion").ToString(),
        "/css/style.css"))%>" rel="stylesheet" />
    <link href="<%=(String.Format("{0}{1}{2}", "https://s3.amazonaws.com/wrdsb-ui-assets/",
        ConfigurationManager.AppSettings("awsVersion").ToString(),
        "/css/icon-styles.css"))%>" rel="stylesheet" />
    <!-- Dot Net specific style just for the login page -->
    <link href="<%=(String.Format("{0}{1}{2}", "https://s3.amazonaws.com/wrdsb-ui-assets/",
        ConfigurationManager.AppSettings("awsVersion").ToString(),
        "/css/dotnetlogin.css"))%>" rel="stylesheet" />
    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script>
    <!-- Include all compiled plugins (below), or include individual files as needed -->
    <script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.1.1/js/bootstrap.min.js"></script>
</head>
<body>
    <div class="login">
        <div id="logo">
            <img src="https://s3.amazonaws.com/wrdsb-theme/images/WRDSB_Logo.svg" />
            <h1>That...didn't work.</h1>
        </div>
        <img src="https://s3.amazonaws.com/wrdsb-theme/images/moss.gif" alt="Error" />
        <div id="loginform">
        <p>Here's what you can do</p>
        <ul>
            <li>Hit that back button and try again</li>
            <li><a href="<%= ConfigurationManager.AppSettings("ownerEmail").ToString()%>">Contact Us</a></li>
            <li>Take me <a href="<%= (ConfigurationManager.AppSettings("appUrl").ToString())%>">Home</a></li>
        </ul>
        </div>
    </div>
</body>
</html>
