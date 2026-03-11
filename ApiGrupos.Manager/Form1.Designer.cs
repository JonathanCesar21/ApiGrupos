namespace ApiGrupos.Manager;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private Label lblExePath;
    private TextBox txtExePath;
    private Button btnBrowseExe;
    private Button btnStartApi;
    private Button btnStopApi;
    private Label lblApiUrl;
    private TextBox txtApiUrl;
    private Label lblUsuario;
    private TextBox txtUsuario;
    private Label lblSenha;
    private TextBox txtSenha;
    private Button btnSalvarCredenciais;
    private Button btnConsultarStatus;
    private Label lblConfigStatus;
    private Label lblProcessStatus;
    private TextBox txtRequestLog;
    private Button btnAtualizarLogs;
    private Label lblLogs;
    private Label lblLogPath;
    private TextBox txtLogPath;
    private Button btnOpenSwagger;
    private OpenFileDialog openFileDialogExe;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblExePath = new Label();
        txtExePath = new TextBox();
        btnBrowseExe = new Button();
        btnStartApi = new Button();
        btnStopApi = new Button();
        lblApiUrl = new Label();
        txtApiUrl = new TextBox();
        lblUsuario = new Label();
        txtUsuario = new TextBox();
        lblSenha = new Label();
        txtSenha = new TextBox();
        btnSalvarCredenciais = new Button();
        btnConsultarStatus = new Button();
        lblConfigStatus = new Label();
        lblProcessStatus = new Label();
        txtRequestLog = new TextBox();
        btnAtualizarLogs = new Button();
        lblLogs = new Label();
        lblLogPath = new Label();
        txtLogPath = new TextBox();
        btnOpenSwagger = new Button();
        openFileDialogExe = new OpenFileDialog();
        SuspendLayout();
        // 
        // lblExePath
        // 
        lblExePath.AutoSize = true;
        lblExePath.Location = new Point(12, 16);
        lblExePath.Name = "lblExePath";
        lblExePath.Size = new Size(126, 15);
        lblExePath.TabIndex = 0;
        lblExePath.Text = "Executavel da API (.exe)";
        // 
        // txtExePath
        // 
        txtExePath.Location = new Point(12, 34);
        txtExePath.Name = "txtExePath";
        txtExePath.Size = new Size(596, 23);
        txtExePath.TabIndex = 1;
        // 
        // btnBrowseExe
        // 
        btnBrowseExe.Location = new Point(614, 33);
        btnBrowseExe.Name = "btnBrowseExe";
        btnBrowseExe.Size = new Size(84, 25);
        btnBrowseExe.TabIndex = 2;
        btnBrowseExe.Text = "Procurar";
        btnBrowseExe.UseVisualStyleBackColor = true;
        btnBrowseExe.Click += btnBrowseExe_Click;
        // 
        // btnStartApi
        // 
        btnStartApi.Location = new Point(704, 33);
        btnStartApi.Name = "btnStartApi";
        btnStartApi.Size = new Size(90, 25);
        btnStartApi.TabIndex = 3;
        btnStartApi.Text = "Iniciar API";
        btnStartApi.UseVisualStyleBackColor = true;
        btnStartApi.Click += btnStartApi_Click;
        // 
        // btnStopApi
        // 
        btnStopApi.Location = new Point(800, 33);
        btnStopApi.Name = "btnStopApi";
        btnStopApi.Size = new Size(90, 25);
        btnStopApi.TabIndex = 4;
        btnStopApi.Text = "Parar API";
        btnStopApi.UseVisualStyleBackColor = true;
        btnStopApi.Click += btnStopApi_Click;
        // 
        // lblApiUrl
        // 
        lblApiUrl.AutoSize = true;
        lblApiUrl.Location = new Point(12, 72);
        lblApiUrl.Name = "lblApiUrl";
        lblApiUrl.Size = new Size(114, 15);
        lblApiUrl.TabIndex = 5;
        lblApiUrl.Text = "URL base da API local";
        // 
        // txtApiUrl
        // 
        txtApiUrl.Location = new Point(12, 90);
        txtApiUrl.Name = "txtApiUrl";
        txtApiUrl.Size = new Size(340, 23);
        txtApiUrl.TabIndex = 6;
        // 
        // lblUsuario
        // 
        lblUsuario.AutoSize = true;
        lblUsuario.Location = new Point(12, 129);
        lblUsuario.Name = "lblUsuario";
        lblUsuario.Size = new Size(69, 15);
        lblUsuario.TabIndex = 7;
        lblUsuario.Text = "Usuario SQL";
        // 
        // txtUsuario
        // 
        txtUsuario.Location = new Point(12, 147);
        txtUsuario.Name = "txtUsuario";
        txtUsuario.Size = new Size(220, 23);
        txtUsuario.TabIndex = 8;
        // 
        // lblSenha
        // 
        lblSenha.AutoSize = true;
        lblSenha.Location = new Point(238, 129);
        lblSenha.Name = "lblSenha";
        lblSenha.Size = new Size(34, 15);
        lblSenha.TabIndex = 9;
        lblSenha.Text = "Senha";
        // 
        // txtSenha
        // 
        txtSenha.Location = new Point(238, 147);
        txtSenha.Name = "txtSenha";
        txtSenha.PasswordChar = '*';
        txtSenha.Size = new Size(220, 23);
        txtSenha.TabIndex = 10;
        // 
        // btnSalvarCredenciais
        // 
        btnSalvarCredenciais.Location = new Point(464, 146);
        btnSalvarCredenciais.Name = "btnSalvarCredenciais";
        btnSalvarCredenciais.Size = new Size(146, 25);
        btnSalvarCredenciais.TabIndex = 11;
        btnSalvarCredenciais.Text = "Salvar credenciais";
        btnSalvarCredenciais.UseVisualStyleBackColor = true;
        btnSalvarCredenciais.Click += btnSalvarCredenciais_Click;
        // 
        // btnConsultarStatus
        // 
        btnConsultarStatus.Location = new Point(616, 146);
        btnConsultarStatus.Name = "btnConsultarStatus";
        btnConsultarStatus.Size = new Size(132, 25);
        btnConsultarStatus.TabIndex = 12;
        btnConsultarStatus.Text = "Consultar status";
        btnConsultarStatus.UseVisualStyleBackColor = true;
        btnConsultarStatus.Click += btnConsultarStatus_Click;
        // 
        // lblConfigStatus
        // 
        lblConfigStatus.AutoSize = true;
        lblConfigStatus.Location = new Point(12, 181);
        lblConfigStatus.Name = "lblConfigStatus";
        lblConfigStatus.Size = new Size(122, 15);
        lblConfigStatus.TabIndex = 13;
        lblConfigStatus.Text = "Status configuracao: -";
        // 
        // lblProcessStatus
        // 
        lblProcessStatus.AutoSize = true;
        lblProcessStatus.Location = new Point(238, 181);
        lblProcessStatus.Name = "lblProcessStatus";
        lblProcessStatus.Size = new Size(93, 15);
        lblProcessStatus.TabIndex = 14;
        lblProcessStatus.Text = "Status processo: -";
        // 
        // txtRequestLog
        // 
        txtRequestLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtRequestLog.Location = new Point(12, 248);
        txtRequestLog.Multiline = true;
        txtRequestLog.Name = "txtRequestLog";
        txtRequestLog.ReadOnly = true;
        txtRequestLog.ScrollBars = ScrollBars.Vertical;
        txtRequestLog.Size = new Size(878, 290);
        txtRequestLog.TabIndex = 15;
        // 
        // btnAtualizarLogs
        // 
        btnAtualizarLogs.Location = new Point(754, 216);
        btnAtualizarLogs.Name = "btnAtualizarLogs";
        btnAtualizarLogs.Size = new Size(136, 25);
        btnAtualizarLogs.TabIndex = 16;
        btnAtualizarLogs.Text = "Atualizar requisicoes";
        btnAtualizarLogs.UseVisualStyleBackColor = true;
        btnAtualizarLogs.Click += btnAtualizarLogs_Click;
        // 
        // lblLogs
        // 
        lblLogs.AutoSize = true;
        lblLogs.Location = new Point(12, 221);
        lblLogs.Name = "lblLogs";
        lblLogs.Size = new Size(120, 15);
        lblLogs.TabIndex = 17;
        lblLogs.Text = "Log de requisicoes API";
        // 
        // lblLogPath
        // 
        lblLogPath.AutoSize = true;
        lblLogPath.Location = new Point(12, 202);
        lblLogPath.Name = "lblLogPath";
        lblLogPath.Size = new Size(99, 15);
        lblLogPath.TabIndex = 18;
        lblLogPath.Text = "Arquivo de log (.txt)";
        // 
        // txtLogPath
        // 
        txtLogPath.Location = new Point(117, 199);
        txtLogPath.Name = "txtLogPath";
        txtLogPath.Size = new Size(631, 23);
        txtLogPath.TabIndex = 19;
        // 
        // btnOpenSwagger
        // 
        btnOpenSwagger.Location = new Point(754, 89);
        btnOpenSwagger.Name = "btnOpenSwagger";
        btnOpenSwagger.Size = new Size(136, 25);
        btnOpenSwagger.TabIndex = 20;
        btnOpenSwagger.Text = "Abrir Swagger";
        btnOpenSwagger.UseVisualStyleBackColor = true;
        btnOpenSwagger.Click += btnOpenSwagger_Click;
        // 
        // openFileDialogExe
        // 
        openFileDialogExe.Filter = "Executavel|*.exe";
        openFileDialogExe.Title = "Selecione o ApiGrupos.exe";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(902, 550);
        Controls.Add(btnOpenSwagger);
        Controls.Add(txtLogPath);
        Controls.Add(lblLogPath);
        Controls.Add(lblLogs);
        Controls.Add(btnAtualizarLogs);
        Controls.Add(txtRequestLog);
        Controls.Add(lblProcessStatus);
        Controls.Add(lblConfigStatus);
        Controls.Add(btnConsultarStatus);
        Controls.Add(btnSalvarCredenciais);
        Controls.Add(txtSenha);
        Controls.Add(lblSenha);
        Controls.Add(txtUsuario);
        Controls.Add(lblUsuario);
        Controls.Add(txtApiUrl);
        Controls.Add(lblApiUrl);
        Controls.Add(btnStopApi);
        Controls.Add(btnStartApi);
        Controls.Add(btnBrowseExe);
        Controls.Add(txtExePath);
        Controls.Add(lblExePath);
        MaximizeBox = false;
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ApiGrupos - Painel de Controle";
        ResumeLayout(false);
        PerformLayout();
    }
}
