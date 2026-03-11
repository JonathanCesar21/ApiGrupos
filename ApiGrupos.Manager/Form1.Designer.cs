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
    private Button btnOpenSwagger;
    private Label lblUsuario;
    private TextBox txtUsuario;
    private Label lblSenha;
    private TextBox txtSenha;
    private Button btnSalvarCredenciais;
    private Button btnConsultarStatus;
    private Label lblConfigStatus;
    private Label lblProcessStatus;
    private Label lblCloudflaredPath;
    private TextBox txtCloudflaredPath;
    private Button btnBrowseCloudflared;
    private Label lblTunnelConfigPath;
    private TextBox txtTunnelConfigPath;
    private Button btnBrowseTunnelConfig;
    private Label lblTunnelName;
    private TextBox txtTunnelName;
    private Button btnStartTunnel;
    private Button btnStopTunnel;
    private Label lblTunnelStatus;
    private Label lblLogPath;
    private TextBox txtLogPath;
    private Label lblLogs;
    private TextBox txtRequestLog;
    private Button btnAtualizarLogs;
    private OpenFileDialog openFileDialogExe;
    private OpenFileDialog openFileDialogCloudflared;
    private OpenFileDialog openFileDialogTunnelConfig;

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
        btnOpenSwagger = new Button();
        lblUsuario = new Label();
        txtUsuario = new TextBox();
        lblSenha = new Label();
        txtSenha = new TextBox();
        btnSalvarCredenciais = new Button();
        btnConsultarStatus = new Button();
        lblConfigStatus = new Label();
        lblProcessStatus = new Label();
        lblCloudflaredPath = new Label();
        txtCloudflaredPath = new TextBox();
        btnBrowseCloudflared = new Button();
        lblTunnelConfigPath = new Label();
        txtTunnelConfigPath = new TextBox();
        btnBrowseTunnelConfig = new Button();
        lblTunnelName = new Label();
        txtTunnelName = new TextBox();
        btnStartTunnel = new Button();
        btnStopTunnel = new Button();
        lblTunnelStatus = new Label();
        lblLogPath = new Label();
        txtLogPath = new TextBox();
        lblLogs = new Label();
        txtRequestLog = new TextBox();
        btnAtualizarLogs = new Button();
        openFileDialogExe = new OpenFileDialog();
        openFileDialogCloudflared = new OpenFileDialog();
        openFileDialogTunnelConfig = new OpenFileDialog();
        SuspendLayout();
        // 
        // lblExePath
        // 
        lblExePath.AutoSize = true;
        lblExePath.Location = new Point(12, 15);
        lblExePath.Name = "lblExePath";
        lblExePath.Size = new Size(126, 15);
        lblExePath.TabIndex = 0;
        lblExePath.Text = "Executavel da API (.exe)";
        // 
        // txtExePath
        // 
        txtExePath.Location = new Point(12, 33);
        txtExePath.Name = "txtExePath";
        txtExePath.Size = new Size(604, 23);
        txtExePath.TabIndex = 1;
        // 
        // btnBrowseExe
        // 
        btnBrowseExe.Location = new Point(622, 32);
        btnBrowseExe.Name = "btnBrowseExe";
        btnBrowseExe.Size = new Size(82, 25);
        btnBrowseExe.TabIndex = 2;
        btnBrowseExe.Text = "Procurar";
        btnBrowseExe.UseVisualStyleBackColor = true;
        btnBrowseExe.Click += btnBrowseExe_Click;
        // 
        // btnStartApi
        // 
        btnStartApi.Location = new Point(710, 32);
        btnStartApi.Name = "btnStartApi";
        btnStartApi.Size = new Size(105, 25);
        btnStartApi.TabIndex = 3;
        btnStartApi.Text = "Iniciar API";
        btnStartApi.UseVisualStyleBackColor = true;
        btnStartApi.Click += btnStartApi_Click;
        // 
        // btnStopApi
        // 
        btnStopApi.Location = new Point(821, 32);
        btnStopApi.Name = "btnStopApi";
        btnStopApi.Size = new Size(105, 25);
        btnStopApi.TabIndex = 4;
        btnStopApi.Text = "Parar API";
        btnStopApi.UseVisualStyleBackColor = true;
        btnStopApi.Click += btnStopApi_Click;
        // 
        // lblApiUrl
        // 
        lblApiUrl.AutoSize = true;
        lblApiUrl.Location = new Point(12, 71);
        lblApiUrl.Name = "lblApiUrl";
        lblApiUrl.Size = new Size(114, 15);
        lblApiUrl.TabIndex = 5;
        lblApiUrl.Text = "URL base da API local";
        // 
        // txtApiUrl
        // 
        txtApiUrl.Location = new Point(12, 89);
        txtApiUrl.Name = "txtApiUrl";
        txtApiUrl.Size = new Size(340, 23);
        txtApiUrl.TabIndex = 6;
        // 
        // btnOpenSwagger
        // 
        btnOpenSwagger.Location = new Point(358, 88);
        btnOpenSwagger.Name = "btnOpenSwagger";
        btnOpenSwagger.Size = new Size(110, 25);
        btnOpenSwagger.TabIndex = 7;
        btnOpenSwagger.Text = "Abrir Swagger";
        btnOpenSwagger.UseVisualStyleBackColor = true;
        btnOpenSwagger.Click += btnOpenSwagger_Click;
        // 
        // lblUsuario
        // 
        lblUsuario.AutoSize = true;
        lblUsuario.Location = new Point(12, 123);
        lblUsuario.Name = "lblUsuario";
        lblUsuario.Size = new Size(69, 15);
        lblUsuario.TabIndex = 8;
        lblUsuario.Text = "Usuario SQL";
        // 
        // txtUsuario
        // 
        txtUsuario.Location = new Point(12, 141);
        txtUsuario.Name = "txtUsuario";
        txtUsuario.Size = new Size(220, 23);
        txtUsuario.TabIndex = 9;
        // 
        // lblSenha
        // 
        lblSenha.AutoSize = true;
        lblSenha.Location = new Point(238, 123);
        lblSenha.Name = "lblSenha";
        lblSenha.Size = new Size(34, 15);
        lblSenha.TabIndex = 10;
        lblSenha.Text = "Senha";
        // 
        // txtSenha
        // 
        txtSenha.Location = new Point(238, 141);
        txtSenha.Name = "txtSenha";
        txtSenha.PasswordChar = '*';
        txtSenha.Size = new Size(220, 23);
        txtSenha.TabIndex = 11;
        // 
        // btnSalvarCredenciais
        // 
        btnSalvarCredenciais.Location = new Point(464, 140);
        btnSalvarCredenciais.Name = "btnSalvarCredenciais";
        btnSalvarCredenciais.Size = new Size(150, 25);
        btnSalvarCredenciais.TabIndex = 12;
        btnSalvarCredenciais.Text = "Salvar credenciais";
        btnSalvarCredenciais.UseVisualStyleBackColor = true;
        btnSalvarCredenciais.Click += btnSalvarCredenciais_Click;
        // 
        // btnConsultarStatus
        // 
        btnConsultarStatus.Location = new Point(620, 140);
        btnConsultarStatus.Name = "btnConsultarStatus";
        btnConsultarStatus.Size = new Size(130, 25);
        btnConsultarStatus.TabIndex = 13;
        btnConsultarStatus.Text = "Consultar status";
        btnConsultarStatus.UseVisualStyleBackColor = true;
        btnConsultarStatus.Click += btnConsultarStatus_Click;
        // 
        // lblConfigStatus
        // 
        lblConfigStatus.AutoSize = true;
        lblConfigStatus.Location = new Point(12, 170);
        lblConfigStatus.Name = "lblConfigStatus";
        lblConfigStatus.Size = new Size(122, 15);
        lblConfigStatus.TabIndex = 14;
        lblConfigStatus.Text = "Status configuracao: -";
        // 
        // lblProcessStatus
        // 
        lblProcessStatus.AutoSize = true;
        lblProcessStatus.Location = new Point(280, 170);
        lblProcessStatus.Name = "lblProcessStatus";
        lblProcessStatus.Size = new Size(93, 15);
        lblProcessStatus.TabIndex = 15;
        lblProcessStatus.Text = "Status processo: -";
        // 
        // lblCloudflaredPath
        // 
        lblCloudflaredPath.AutoSize = true;
        lblCloudflaredPath.Location = new Point(12, 196);
        lblCloudflaredPath.Name = "lblCloudflaredPath";
        lblCloudflaredPath.Size = new Size(164, 15);
        lblCloudflaredPath.TabIndex = 16;
        lblCloudflaredPath.Text = "Cloudflared executavel (.exe)";
        // 
        // txtCloudflaredPath
        // 
        txtCloudflaredPath.Location = new Point(12, 214);
        txtCloudflaredPath.Name = "txtCloudflaredPath";
        txtCloudflaredPath.Size = new Size(604, 23);
        txtCloudflaredPath.TabIndex = 17;
        // 
        // btnBrowseCloudflared
        // 
        btnBrowseCloudflared.Location = new Point(622, 213);
        btnBrowseCloudflared.Name = "btnBrowseCloudflared";
        btnBrowseCloudflared.Size = new Size(82, 25);
        btnBrowseCloudflared.TabIndex = 18;
        btnBrowseCloudflared.Text = "Procurar";
        btnBrowseCloudflared.UseVisualStyleBackColor = true;
        btnBrowseCloudflared.Click += btnBrowseCloudflared_Click;
        // 
        // lblTunnelConfigPath
        // 
        lblTunnelConfigPath.AutoSize = true;
        lblTunnelConfigPath.Location = new Point(12, 246);
        lblTunnelConfigPath.Name = "lblTunnelConfigPath";
        lblTunnelConfigPath.Size = new Size(149, 15);
        lblTunnelConfigPath.TabIndex = 19;
        lblTunnelConfigPath.Text = "Config do tunnel (config.yml)";
        // 
        // txtTunnelConfigPath
        // 
        txtTunnelConfigPath.Location = new Point(12, 264);
        txtTunnelConfigPath.Name = "txtTunnelConfigPath";
        txtTunnelConfigPath.Size = new Size(604, 23);
        txtTunnelConfigPath.TabIndex = 20;
        // 
        // btnBrowseTunnelConfig
        // 
        btnBrowseTunnelConfig.Location = new Point(622, 263);
        btnBrowseTunnelConfig.Name = "btnBrowseTunnelConfig";
        btnBrowseTunnelConfig.Size = new Size(82, 25);
        btnBrowseTunnelConfig.TabIndex = 21;
        btnBrowseTunnelConfig.Text = "Procurar";
        btnBrowseTunnelConfig.UseVisualStyleBackColor = true;
        btnBrowseTunnelConfig.Click += btnBrowseTunnelConfig_Click;
        // 
        // lblTunnelName
        // 
        lblTunnelName.AutoSize = true;
        lblTunnelName.Location = new Point(12, 296);
        lblTunnelName.Name = "lblTunnelName";
        lblTunnelName.Size = new Size(91, 15);
        lblTunnelName.TabIndex = 22;
        lblTunnelName.Text = "Nome do tunnel";
        // 
        // txtTunnelName
        // 
        txtTunnelName.Location = new Point(12, 314);
        txtTunnelName.Name = "txtTunnelName";
        txtTunnelName.Size = new Size(220, 23);
        txtTunnelName.TabIndex = 23;
        // 
        // btnStartTunnel
        // 
        btnStartTunnel.Location = new Point(238, 313);
        btnStartTunnel.Name = "btnStartTunnel";
        btnStartTunnel.Size = new Size(130, 25);
        btnStartTunnel.TabIndex = 24;
        btnStartTunnel.Text = "Iniciar Tunnel";
        btnStartTunnel.UseVisualStyleBackColor = true;
        btnStartTunnel.Click += btnStartTunnel_Click;
        // 
        // btnStopTunnel
        // 
        btnStopTunnel.Location = new Point(374, 313);
        btnStopTunnel.Name = "btnStopTunnel";
        btnStopTunnel.Size = new Size(130, 25);
        btnStopTunnel.TabIndex = 25;
        btnStopTunnel.Text = "Parar Tunnel";
        btnStopTunnel.UseVisualStyleBackColor = true;
        btnStopTunnel.Click += btnStopTunnel_Click;
        // 
        // lblTunnelStatus
        // 
        lblTunnelStatus.AutoSize = true;
        lblTunnelStatus.Location = new Point(510, 318);
        lblTunnelStatus.Name = "lblTunnelStatus";
        lblTunnelStatus.Size = new Size(81, 15);
        lblTunnelStatus.TabIndex = 26;
        lblTunnelStatus.Text = "Status tunnel: -";
        // 
        // lblLogPath
        // 
        lblLogPath.AutoSize = true;
        lblLogPath.Location = new Point(12, 347);
        lblLogPath.Name = "lblLogPath";
        lblLogPath.Size = new Size(99, 15);
        lblLogPath.TabIndex = 27;
        lblLogPath.Text = "Arquivo de log (.txt)";
        // 
        // txtLogPath
        // 
        txtLogPath.Location = new Point(12, 365);
        txtLogPath.Name = "txtLogPath";
        txtLogPath.Size = new Size(737, 23);
        txtLogPath.TabIndex = 28;
        // 
        // lblLogs
        // 
        lblLogs.AutoSize = true;
        lblLogs.Location = new Point(12, 394);
        lblLogs.Name = "lblLogs";
        lblLogs.Size = new Size(120, 15);
        lblLogs.TabIndex = 29;
        lblLogs.Text = "Log de requisicoes API";
        // 
        // txtRequestLog
        // 
        txtRequestLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtRequestLog.Location = new Point(12, 412);
        txtRequestLog.Multiline = true;
        txtRequestLog.Name = "txtRequestLog";
        txtRequestLog.ReadOnly = true;
        txtRequestLog.ScrollBars = ScrollBars.Vertical;
        txtRequestLog.Size = new Size(914, 220);
        txtRequestLog.TabIndex = 30;
        // 
        // btnAtualizarLogs
        // 
        btnAtualizarLogs.Location = new Point(755, 364);
        btnAtualizarLogs.Name = "btnAtualizarLogs";
        btnAtualizarLogs.Size = new Size(171, 25);
        btnAtualizarLogs.TabIndex = 31;
        btnAtualizarLogs.Text = "Atualizar requisicoes";
        btnAtualizarLogs.UseVisualStyleBackColor = true;
        btnAtualizarLogs.Click += btnAtualizarLogs_Click;
        // 
        // openFileDialogExe
        // 
        openFileDialogExe.Filter = "Executavel|*.exe";
        openFileDialogExe.Title = "Selecione o ApiGrupos.exe";
        // 
        // openFileDialogCloudflared
        // 
        openFileDialogCloudflared.Filter = "Executavel|*.exe";
        openFileDialogCloudflared.Title = "Selecione o cloudflared.exe";
        // 
        // openFileDialogTunnelConfig
        // 
        openFileDialogTunnelConfig.Filter = "Arquivo YAML|*.yml;*.yaml";
        openFileDialogTunnelConfig.Title = "Selecione o config.yml do tunnel";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(940, 644);
        Controls.Add(btnAtualizarLogs);
        Controls.Add(txtRequestLog);
        Controls.Add(lblLogs);
        Controls.Add(txtLogPath);
        Controls.Add(lblLogPath);
        Controls.Add(lblTunnelStatus);
        Controls.Add(btnStopTunnel);
        Controls.Add(btnStartTunnel);
        Controls.Add(txtTunnelName);
        Controls.Add(lblTunnelName);
        Controls.Add(btnBrowseTunnelConfig);
        Controls.Add(txtTunnelConfigPath);
        Controls.Add(lblTunnelConfigPath);
        Controls.Add(btnBrowseCloudflared);
        Controls.Add(txtCloudflaredPath);
        Controls.Add(lblCloudflaredPath);
        Controls.Add(lblProcessStatus);
        Controls.Add(lblConfigStatus);
        Controls.Add(btnConsultarStatus);
        Controls.Add(btnSalvarCredenciais);
        Controls.Add(txtSenha);
        Controls.Add(lblSenha);
        Controls.Add(txtUsuario);
        Controls.Add(lblUsuario);
        Controls.Add(btnOpenSwagger);
        Controls.Add(txtApiUrl);
        Controls.Add(lblApiUrl);
        Controls.Add(btnStopApi);
        Controls.Add(btnStartApi);
        Controls.Add(btnBrowseExe);
        Controls.Add(txtExePath);
        Controls.Add(lblExePath);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ApiGrupos - Painel de Controle";
        ResumeLayout(false);
        PerformLayout();
    }
}
